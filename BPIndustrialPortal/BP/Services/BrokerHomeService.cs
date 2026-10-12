using System.Xml.Linq;
using BP.IService;
using BP.Models;
using ClosedXML.Excel;

namespace BP.Services
{
    public class BrokerHomeService : IBrokerHomeService
    {
        private readonly string _metricsFilePath;
        private readonly string _renewalWatchlistFilePath;
        private readonly string _openSupportRequestsFilePath;

        public BrokerHomeService(IWebHostEnvironment env)
        {
            var dataPath = Path.Combine(env.ContentRootPath, "Data");
            _metricsFilePath = Path.Combine(dataPath, "broker-home-metrics.xml");
            _renewalWatchlistFilePath = Path.Combine(dataPath, "broker-renewal-watchlist.xml");
            _openSupportRequestsFilePath = Path.Combine(dataPath, "broker-open-support-requests.xml");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private XDocument LoadMetricsXml()
        {
            if (!File.Exists(_metricsFilePath))
                throw new FileNotFoundException($"Broker home metrics data file not found: {_metricsFilePath}");
            return XDocument.Load(_metricsFilePath);
        }

        private (DateTime lastUpdated, List<XElement> items) LoadRenewalWatchlistXml()
        {
            if (!File.Exists(_renewalWatchlistFilePath))
                throw new FileNotFoundException($"Broker renewal watchlist data file not found: {_renewalWatchlistFilePath}");

            var doc = XDocument.Load(_renewalWatchlistFilePath);
            var lastUpdated = DateTime.Parse((string)doc.Root!.Element("LastUpdated")!);
            var items = doc.Root.Element("Items")!.Elements("Item").ToList();
            return (lastUpdated, items);
        }

        private List<XElement> LoadOpenSupportRequestsXml()
        {
            if (!File.Exists(_openSupportRequestsFilePath))
                throw new FileNotFoundException($"Broker open support requests data file not found: {_openSupportRequestsFilePath}");

            var doc = XDocument.Load(_openSupportRequestsFilePath);
            return doc.Root!.Element("Items")!.Elements("Item").ToList();
        }

        private static RenewalWatchlistEntry MapWatchlistEntry(XElement e) => new()
        {
            Customer = (string)e.Element("Customer")!,
            BrokerMargin = (decimal)e.Element("BrokerMargin")!,
            TermMonths = (int)e.Element("TermMonths")!,
            VolumeMWh = (decimal)e.Element("VolumeMWh")!,
            DaysRemaining = (int)e.Element("DaysRemaining")!
        };

        private static BrokerSupportRequestEntry MapSupportEntry(XElement e) => new()
        {
            Customer = (string)e.Element("Customer")!,
            RequestType = (string)e.Element("RequestType")!,
            Priority = (string)e.Element("Priority")!,
            CreatedOn = (string)e.Element("CreatedOn")!
        };

        private static IEnumerable<RenewalWatchlistEntry> ApplyDaysFilter(
            IEnumerable<RenewalWatchlistEntry> items, string daysFilter) =>
            daysFilter.ToLower() switch
            {
                "0-15" => items.Where(i => i.DaysRemaining >= 0 && i.DaysRemaining <= 15),
                "16-30" => items.Where(i => i.DaysRemaining >= 16 && i.DaysRemaining <= 30),
                "31-60" => items.Where(i => i.DaysRemaining >= 31 && i.DaysRemaining <= 60),
                "61-90" => items.Where(i => i.DaysRemaining >= 61 && i.DaysRemaining <= 90),
                _ => items
            };

        private static List<RenewalWatchlistEntry> ApplyWatchlistSort(
            IEnumerable<RenewalWatchlistEntry> items, string sortBy, string sortOrder)
        {
            bool asc = !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "brokermargin" => asc ? items.OrderBy(i => i.BrokerMargin).ToList()
                                      : items.OrderByDescending(i => i.BrokerMargin).ToList(),
                "term" => asc ? items.OrderBy(i => i.TermMonths).ToList()
                              : items.OrderByDescending(i => i.TermMonths).ToList(),
                "volume" => asc ? items.OrderBy(i => i.VolumeMWh).ToList()
                                : items.OrderByDescending(i => i.VolumeMWh).ToList(),
                "daysremaining" => asc ? items.OrderBy(i => i.DaysRemaining).ToList()
                                       : items.OrderByDescending(i => i.DaysRemaining).ToList(),
                _ => asc ? items.OrderBy(i => i.Customer).ToList()
                         : items.OrderByDescending(i => i.Customer).ToList()
            };
        }

        private static List<BrokerSupportRequestEntry> ApplySupportSort(
            IEnumerable<BrokerSupportRequestEntry> items, string sortBy, string sortOrder)
        {
            bool asc = !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "requesttype" => asc ? items.OrderBy(i => i.RequestType).ToList()
                                     : items.OrderByDescending(i => i.RequestType).ToList(),
                "priority" => asc ? items.OrderBy(i => PriorityRank(i.Priority)).ToList()
                                  : items.OrderByDescending(i => PriorityRank(i.Priority)).ToList(),
                "createdon" => asc ? items.OrderBy(i => ParseDate(i.CreatedOn)).ToList()
                                   : items.OrderByDescending(i => ParseDate(i.CreatedOn)).ToList(),
                _ => asc ? items.OrderBy(i => i.Customer).ToList()
                         : items.OrderByDescending(i => i.Customer).ToList()
            };
        }

        private static int PriorityRank(string priority) => priority.ToUpper() switch
        {
            "HIGH" => 1,
            "MEDIUM" => 2,
            "LOW" => 3,
            _ => 4
        };

        private static DateTime ParseDate(string value) =>
            DateTime.TryParse(value, out var d) ? d : DateTime.MinValue;

        // ── Public methods ─────────────────────────────────────────────────────

        public BrokerSummaryMetrics GetSummaryMetrics()
        {
            var doc = LoadMetricsXml();
            var root = doc.Root!;
            var lastUpdated = DateTime.Parse((string)root.Element("LastUpdated")!);
            var sm = root.Element("SummaryMetrics")!;

            var cm = sm.Element("CustomersManaged")!;
            var ac = sm.Element("ActiveContracts")!;
            var ur = sm.Element("UpcomingRenewals")!;
            var mm = sm.Element("MetersManaged")!;
            var au = sm.Element("AnnualUsage")!;

            int newThisMonth = (int)cm.Element("NewThisMonth")!;
            int churned = (int)cm.Element("Churned")!;
            int fixedContracts = (int)ac.Element("Fixed")!;
            int variableContracts = (int)ac.Element("Variable")!;
            int pending = (int)ur.Element("Pending")!;
            int avgDays = (int)ur.Element("AvgDaysToExpiry")!;
            string yoyChange = (string)au.Element("YoYChange")!;

            return new BrokerSummaryMetrics
            {
                LastUpdated = lastUpdated,
                CustomersManaged = new BrokerCustomersManaged
                {
                    Total = (int)cm.Element("Total")!,
                    NewThisMonth = newThisMonth,
                    NewThisMonthLabel = $"+{newThisMonth} this month",
                    Churned = churned,
                    ChurnedLabel = $"{churned} churned"
                },
                ActiveContracts = new BrokerActiveContracts
                {
                    Total = (int)ac.Element("Total")!,
                    Fixed = fixedContracts,
                    Variable = variableContracts,
                    BreakdownLabel = $"Fixed {fixedContracts} · Var {variableContracts}"
                },
                UpcomingRenewals = new BrokerUpcomingRenewals
                {
                    Total = (int)ur.Element("Total")!,
                    Pending = pending,
                    PendingLabel = $"Pending · {pending}",
                    AvgDaysToExpiry = avgDays,
                    AvgExpiryLabel = $"Avg. {avgDays}d expiry"
                },
                MetersManaged = new BrokerMetersManaged
                {
                    Total = (int)mm.Element("Total")!,
                    Decreased = (int)mm.Element("Decreased")!,
                    Increased = (int)mm.Element("Increased")!
                },
                AnnualUsage = new BrokerAnnualUsage
                {
                    TotalMWh = (decimal)au.Element("TotalMWh")!,
                    YoYChange = yoyChange,
                    YoYLabel = $"{yoyChange} YoY"
                }
            };
        }

        public BrokerRecentActivity GetRecentActivity()
        {
            var doc = LoadMetricsXml();
            var root = doc.Root!;
            var lastUpdated = DateTime.Parse((string)root.Element("LastUpdated")!);
            var ra = root.Element("RecentActivity")!;

            var cas = ra.Element("ContractAmendmentsSigned")!;
            var osr = ra.Element("OpenSupportRequests")!;
            var ucr = ra.Element("UpcomingContractRenewals")!;
            var era = ra.Element("EstimatedRevenueAtRenewal")!;

            int highPriority = (int)osr.Element("HighPriority")!;
            int dueWithinDays = (int)ucr.Element("DueWithinDays")!;
            int renewalCount = (int)era.Element("RenewalCount")!;

            return new BrokerRecentActivity
            {
                LastUpdated = lastUpdated,
                ContractAmendmentsSigned = new BrokerContractAmendments
                {
                    Count = (int)cas.Element("Count")!,
                    Label = (string)cas.Element("Label")!
                },
                OpenSupportRequests = new BrokerOpenSupportRequestsSummary
                {
                    Count = (int)osr.Element("Count")!,
                    HighPriority = highPriority,
                    Label = $"{highPriority} High priority"
                },
                UpcomingContractRenewals = new BrokerUpcomingRenewalsSummary
                {
                    Count = (int)ucr.Element("Count")!,
                    DueWithinDays = dueWithinDays,
                    Label = $"Due within the next {dueWithinDays} days"
                },
                EstimatedRevenueAtRenewal = new BrokerEstimatedRevenue
                {
                    Amount = (decimal)era.Element("Amount")!,
                    RenewalCount = renewalCount,
                    Label = $"Across {renewalCount} upcoming renewals"
                }
            };
        }

        public RenewalWatchlistResponse GetRenewalWatchlist(string daysFilter, string sortBy, string sortOrder)
        {
            var (_, elements) = LoadRenewalWatchlistXml();
            var allItems = elements.Select(MapWatchlistEntry).ToList();

            var filtered = ApplyDaysFilter(allItems, daysFilter).ToList();
            var sorted = ApplyWatchlistSort(filtered, sortBy, sortOrder);

            var filters = new List<RenewalWatchlistFilterOption>
            {
                new() { Label = $"All remaining ({allItems.Count})", Value = "all", Count = allItems.Count },
                new() { Label = $"0-15 days ({allItems.Count(i => i.DaysRemaining <= 15)})", Value = "0-15", Count = allItems.Count(i => i.DaysRemaining <= 15) },
                new() { Label = $"16-30 days ({allItems.Count(i => i.DaysRemaining >= 16 && i.DaysRemaining <= 30)})", Value = "16-30", Count = allItems.Count(i => i.DaysRemaining >= 16 && i.DaysRemaining <= 30) },
                new() { Label = $"31-60 days ({allItems.Count(i => i.DaysRemaining >= 31 && i.DaysRemaining <= 60)})", Value = "31-60", Count = allItems.Count(i => i.DaysRemaining >= 31 && i.DaysRemaining <= 60) },
                new() { Label = $"61-90 days ({allItems.Count(i => i.DaysRemaining >= 61 && i.DaysRemaining <= 90)})", Value = "61-90", Count = allItems.Count(i => i.DaysRemaining >= 61 && i.DaysRemaining <= 90) }
            };

            return new RenewalWatchlistResponse
            {
                TotalEntries = sorted.Count,
                Filters = filters,
                Items = sorted
            };
        }

        public byte[] ExportRenewalWatchlist(string daysFilter)
        {
            var (_, elements) = LoadRenewalWatchlistXml();
            var allItems = elements.Select(MapWatchlistEntry).ToList();
            var filtered = ApplyDaysFilter(allItems, daysFilter).OrderBy(i => i.DaysRemaining).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Renewal Watchlist");

            // ── Header row ────────────────────────────────────────────────────
            string[] headers = ["Customer", "Broker Margin", "Term (Month)", "Volume (MWh)", "Days Remaining"];
            var bpGreen = XLColor.FromHtml("#007A33");
            var headerBorder = XLColor.FromHtml("#005C26");

            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(1, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Font.FontSize = 11;
                cell.Style.Fill.BackgroundColor = bpGreen;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = headerBorder;
            }

            // ── Data rows ─────────────────────────────────────────────────────
            var altRow = XLColor.FromHtml("#F0FAF4");
            var dataBorder = XLColor.FromHtml("#CCCCCC");

            for (int i = 0; i < filtered.Count; i++)
            {
                var item = filtered[i];
                int row = i + 2;
                var rowBg = i % 2 == 0 ? XLColor.White : altRow;

                ws.Cell(row, 1).Value = item.Customer;
                ws.Cell(row, 2).Value = item.BrokerMargin;
                ws.Cell(row, 3).Value = item.TermMonths;
                ws.Cell(row, 4).Value = item.VolumeMWh;
                ws.Cell(row, 5).Value = item.DaysRemaining;

                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.000";

                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(row, col);
                    cell.Style.Fill.BackgroundColor = rowBg;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = dataBorder;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }

            // ── Polish ────────────────────────────────────────────────────────
            ws.Columns().AdjustToContents();
            ws.Row(1).Height = 20;
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public BrokerOpenSupportRequestsResponse GetOpenSupportRequests(string sortBy, string sortOrder)
        {
            var elements = LoadOpenSupportRequestsXml();
            var items = elements.Select(MapSupportEntry).ToList();
            var sorted = ApplySupportSort(items, sortBy, sortOrder);

            return new BrokerOpenSupportRequestsResponse
            {
                TotalEntries = sorted.Count,
                Items = sorted
            };
        }
    }
}
