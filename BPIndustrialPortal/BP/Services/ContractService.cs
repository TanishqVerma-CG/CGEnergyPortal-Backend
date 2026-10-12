using BP.IService;
using BP.Models;
using System.Diagnostics.Contracts;
using System.Xml.Linq;

namespace BP.Services
{
    public class ContractService : IContractService
    {
        private readonly string _xmlFilePath;
        private readonly string _xmlHistoryFilePath;
        private readonly string _xmlFilePathContractsTable;
        private readonly string _xmlFilePathContractsRenew;
        private readonly string _xmlFilePathPricingIntelligence;
        private readonly string _xmlFilePathPricingTrend;
        private readonly string _xmlFilePathPricingSnapshotDetails;
        private readonly string _xmlFilePathFormalQuotes;
        private readonly string _xmlFilePathRenewals;
        private readonly ISupportCaseService _supportCaseService;

        public ContractService(IWebHostEnvironment env, ISupportCaseService supportCaseService)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "contracts.xml");
            _xmlHistoryFilePath = Path.Combine(env.ContentRootPath, "Data", "contract-history.xml");
            _xmlFilePathContractsTable = Path.Combine(env.ContentRootPath, "Data", "contracts-table.xml");
            _xmlFilePathContractsRenew = Path.Combine(env.ContentRootPath, "Data", "contracts-renew.xml");
            _xmlFilePathPricingIntelligence = Path.Combine(env.ContentRootPath, "Data", "pricing-intelligence.xml");
            _xmlFilePathPricingTrend = Path.Combine(env.ContentRootPath, "Data", "pricing-trend.xml");
            _xmlFilePathPricingSnapshotDetails = Path.Combine(env.ContentRootPath, "Data", "pricing-snapshot-details.xml");
            _xmlFilePathFormalQuotes = Path.Combine(env.ContentRootPath, "Data", "formal-quotes.xml");
            _xmlFilePathRenewals = Path.Combine(env.ContentRootPath, "Data", "renewals.xml");
            _supportCaseService = supportCaseService;
        }

        private (DateTime lastUpdated, List<XElement> elements) LoadXml()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Contract data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var lastUpdated = DateTime.Parse((string)doc.Root!.Element("LastUpdated")!);
            var elements = doc.Root.Element("Contracts")!.Elements("Contract").ToList();
            return (lastUpdated, elements);
        }

        private List<XElement> LoadContractsTableXml()
        {
            if (!File.Exists(_xmlFilePathContractsTable))
                throw new FileNotFoundException($"Contract data file not found: {_xmlFilePathContractsTable}");

            var doc = XDocument.Load(_xmlFilePathContractsTable);
            var elements = doc.Root!.Element("Contracts")!.Elements("Contract").ToList();
            return elements;
        }

        private static ContractSummary MapToSummary(XElement e, DateTime lastUpdated)
        {
            var ec = e.Element("ExpiringContract")!;
            var cu = e.Element("ContractUtilization")!;
            var exp = e.Element("ExpiringIn")!;
            var am = e.Element("ActiveMeter")!;

            return new ContractSummary
            {
                ContractId = (string)e.Element("ContractId")!,
                Label = (string)e.Element("Label")!,
                LastUpdated = lastUpdated,

                ActiveContracts = new ActiveContractsInfo
                {
                    Count = (int)e.Element("ActiveContracts")!
                },

                TotalMeter = new TotalMeterInfo
                {
                    Count = (int)e.Element("TotalMeter")!,
                    ActiveMeterCount = (int)am.Element("Count")!
                },

                TotalContractVolume = new TotalContractVolumeInfo
                {
                    VolumeMWh = (decimal)e.Element("TotalContractVolumeMWh")!,
                    ContractedMWh = (decimal)cu.Element("ContractedMWh")!
                },

                ExpiringIn30Days = new ExpiringIn30DaysInfo
                {
                    Count = (int)e.Element("ExpiringIn30Days")!
                },

                ExpiringContract = new ExpiringContractInfo
                {
                    Count = (int)ec.Element("Count")!,
                    DaysLabel = (string?)ec.Element("DaysLabel") ?? string.Empty
                },

                ContractUtilization = new ContractUtilizationInfo
                {
                    Count = (int)cu.Element("Count")!,
                    UsedMWh = (decimal)cu.Element("UsedMWh")!,
                    ContractedMWh = (decimal)cu.Element("ContractedMWh")!
                },

                ExpiringIn = new ExpiringInInfo
                {
                    Days = (int)exp.Element("Days")!,
                    ExpiryDate = (string?)exp.Element("ExpiryDate") ?? string.Empty
                },

                OpenQuoteRequests = new OpenQuoteRequestsInfo
                {
                    Count = (int?)e.Element("OpenQuoteRequests") ?? 0
                },

                QuotesApproved = new QuotesApprovedInfo
                {
                    Count = (int?)e.Element("QuotesApproved") ?? 0
                }
            };
        }

        public ContractDropdownResponse GetContractDropdown()
        {
            var (lastUpdated, elements) = LoadXml();

            return new ContractDropdownResponse
            {
                LastUpdated = lastUpdated,
                Contracts = elements
           .Select(e => new ContractDropdownItem
           {
               ContractId = (string)e.Element("ContractId")!,
               Label = (string)e.Element("Label")!
           })
                .ToList()
            };
        }

        public ContractSummary? GetContractSummary(string contractId)
        {
            var (lastUpdated, elements) = LoadXml();

            var element = elements.FirstOrDefault(e =>
                ((string)e.Element("ContractId")!)
               .Equals(contractId, StringComparison.OrdinalIgnoreCase));

            return element is null ? null : MapToSummary(element, lastUpdated);
        }

        public List<ContractHistoryEntry>? GetContractHistory(string contractId)
        {
            if (!File.Exists(_xmlHistoryFilePath))
                throw new FileNotFoundException($"Contract history data file not found: {_xmlHistoryFilePath}");

            var doc = XDocument.Load(_xmlHistoryFilePath);
            var contractEl = doc.Root!
                .Element("Contracts")!
                .Elements("Contract")
                .FirstOrDefault(e => ((string)e.Element("ContractId")!)
                    .Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (contractEl is null) return null;

            return contractEl
                .Element("History")!
                .Elements("Entry")
                .Select(e => new ContractHistoryEntry
                {
                    EntryId = (string?)e.Element("EntryId") ?? string.Empty,
                    EffectiveDate = (string?)e.Element("EffectiveDate") ?? string.Empty,
                    EventType = (string?)e.Element("EventType") ?? string.Empty,
                    Description = (string?)e.Element("Description") ?? string.Empty,
                    DocumentName = (string?)e.Element("DocumentName") ?? string.Empty,
                    DocumentUrl = (string?)e.Element("DocumentUrl") ?? string.Empty
                })
                .ToList();
        }

        public ContractDetail? GetContractDetailById(string id)
        {
            var elements = LoadContractsTableXml();

            var element = elements.FirstOrDefault(e =>
                ((string)e.Element("ContractId")!)
               .Equals(id, StringComparison.OrdinalIgnoreCase));

            if (element is null)
                return null;

            return new ContractDetail
            {
                ContractId = (string)element.Element("ContractId")!,
                Documents = element.Element("Documents")?.Elements("Document").Select(d => new ContractDocument
                {
                    DocumentId = (int?)d.Element("DocumentId") ?? 0,
                    DocumentName = (string?)d.Element("DocumentName") ?? string.Empty,
                    Type = (string?)d.Element("Type") ?? string.Empty,
                    SignedDate = (string?)d.Element("SignedDate") ?? string.Empty
                }).ToList() ?? []
            };
        }

        public MeterTableResponse? GetMetersByContractId(string contractId)
        {
            var metersFilePath = Path.Combine(
                Path.GetDirectoryName(_xmlFilePath)!, "contracts-meters.xml");

            if (!File.Exists(metersFilePath))
                throw new FileNotFoundException($"Contracts meters data file not found: {metersFilePath}");

            var doc = XDocument.Load(metersFilePath);
            var contractMeter = doc.Root!
                .Element("ContractMeters")!
                .Elements("ContractMeter")
                .FirstOrDefault(e =>
                    ((string)e.Element("ContractId")!)
                    .Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (contractMeter is null)
                return null;

            var meters = contractMeter
                .Element("Meters")!
                .Elements("Meter")
                .Select(e => new MeterTableRow
                {
                    MeterId = (string)e.Element("MeterId")!,
                    UAN = (string?)e.Element("UAN") ?? string.Empty,
                    LDC = (string)e.Element("LDC")!,
                    Utility = (string)e.Element("Utility")!,
                    ServiceAddress = (string)e.Element("ServiceAddress")!,
                    Status = (string)e.Element("Status")!,
                    StartDate = (string)e.Element("StartDate")!,
                    StopDate = (string?)e.Element("StopDate") ?? string.Empty,
                    AnnualUsage = (long)e.Element("AnnualUsage")!
                })
                .ToList();

            return new MeterTableResponse
            {
                ContractId = contractId,
                Meters = meters
            };
        }

        public ContractTableResponse GetContracts()
        {
            var elements = LoadContractsTableXml();

            var metersFilePath = Path.Combine(Path.GetDirectoryName(_xmlFilePath)!, "contracts-meters.xml");
            var meterCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(metersFilePath))
            {
                meterCountMap = XDocument.Load(metersFilePath).Root!.Element("ContractMeters")!.Elements("ContractMeter").ToDictionary(e => (string)e.Element("ContractId")!, e => e.Element("Meters")!.Elements("Meter").Count(), StringComparer.OrdinalIgnoreCase);
            }

            var contracts = elements.Select(e =>
            {
                var id = (string)e.Element("ContractId")!;
                return new ContractTableRow
                {
                    ContractId = id,
                    ContractType = (string)e.Element("ContractType")!,
                    MeterCount = meterCountMap.TryGetValue(id, out var count) ? count : 0,
                    Amendments = (int)e.Element("Amendments")!,
                    Status = (string)e.Element("Status")!,
                    StartDate = (string)e.Element("StartDate")!,
                    EndDate = (string)e.Element("EndDate")!
                };
            }).ToList();

            return new ContractTableResponse { Contracts = contracts };
        }

        public RenewContractsResponse GetRenewContracts()
        {
            if (!File.Exists(_xmlFilePathContractsRenew))
                throw new FileNotFoundException($"Contract renewal data file not found: {_xmlFilePathContractsRenew}");

            var doc = XDocument.Load(_xmlFilePathContractsRenew);
            var root = doc.Root!;

            var addresses = root
                .Element("ServiceAddresses")!
                .Elements("Address")
                .Select(a => new RenewContractAddress
                {
                    Id = (string)a.Attribute("id")!,
                    Address = (string)a
                })
                .ToList();

            var expiringIds = root
                .Element("ExpiringContracts")!
                .Elements("ContractId")
                .Select(e => (string)e)
                .ToList();

            var plans = root
                .Element("Plans")!
                .Elements("Plan")
                .Select(p => new RenewContractPlan
                {
                    TermMonths = (int)p.Attribute("termMonths")!,
                    Badge = (string?)p.Element("Badge"),
                    Rate = (decimal)p.Element("Rate")!,
                    RateUnit = (string?)p.Element("Rate")?.Attribute("unit") ?? string.Empty,
                    UsageBasis = (decimal)p.Element("UsageBasis")!,
                    RateType = (string)p.Element("RateType")!,
                    BaseCharge = (decimal)p.Element("BaseCharge")!,
                    BaseChargeUnit = (string?)p.Element("BaseCharge")?.Attribute("unit") ?? string.Empty
                })
                .ToList();

            return new RenewContractsResponse
            {
                ServiceAddresses = addresses,
                ExpiringContractIds = expiringIds,
                Plans = plans
            };
        }

        public PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot, string priceUnit)
        {
            if (!File.Exists(_xmlFilePathPricingIntelligence))
                throw new FileNotFoundException($"Pricing intelligence data file not found: {_xmlFilePathPricingIntelligence}");

            var doc = XDocument.Load(_xmlFilePathPricingIntelligence);

            var contractEl = doc.Root!.Elements("ContractPricing").FirstOrDefault(e => ((string)e.Attribute("contractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (contractEl is null)
                return null;

            var snapshotEl = contractEl.Elements("Snapshot").FirstOrDefault(s => ((string)s.Attribute("id")!).Equals(snapshot, StringComparison.OrdinalIgnoreCase));

            if (snapshotEl is null)
                return null;

            var termCols = snapshotEl.Element("TermColumns")!.Elements("Term").Select(t => new PricingTermColumn
            {
                TermMonths = (int)t.Attribute("months")!,
                IsRecommended = (bool)t.Attribute("recommended")!
            }).ToList();

            bool convertToKwh = priceUnit.Equals("$/kWh", StringComparison.OrdinalIgnoreCase);

            var rows = snapshotEl.Element("Rows")!.Elements("Row")
                .Select(r =>
                {
                    var priceMap = r.Elements("Price")
                    .ToDictionary(p => (int)p.Attribute("term")!,
                          p => new PricingCell
                          {
                              Price = convertToKwh
                                  ? Math.Round((decimal)p.Attribute("value")! / 1000m, 5)
                                  : (decimal)p.Attribute("value")!,
                              PriceRange = (string)p.Attribute("range")!
                          });

                    return new PricingMatrixRow
                    {
                        Label = (string)r.Attribute("label")!,
                        Prices = termCols.Select(tc => priceMap.TryGetValue(tc.TermMonths, out var cell) ? cell : new PricingCell()).ToList()
                    };
                }).ToList();

            return new PricingIntelligenceResponse
            {
                ContractId = contractId,
                PriceUnit = priceUnit,
                Snapshot = (string)snapshotEl.Attribute("id")!,
                SnapshotLabel = (string)snapshotEl.Attribute("label")!,
                TermColumns = termCols,
                Rows = rows
            };
        }

        public PricingTrendResponse? GetPricingTrend(string contractId)
        {
            if (!File.Exists(_xmlFilePathPricingTrend))
                throw new FileNotFoundException($"Pricing trend data file not found: {_xmlFilePathPricingTrend}");

            var doc = XDocument.Load(_xmlFilePathPricingTrend);

            var trendEl = doc.Root!.Elements("ContractTrend")
                .FirstOrDefault(e => ((string)e.Attribute("contractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (trendEl is null)
                return null;

            var series = trendEl.Elements("Series").Select(s => new PricingTrendSeries
            {
                TermMonths = (int)s.Attribute("termMonths")!,
                DataPoints = s.Elements("Point").Select(p => new PricingTrendDataPoint
                {
                    Date = (string)p.Attribute("date")!,
                    Price = (decimal)p.Attribute("value")!,
                    IsToday = (bool)p.Attribute("isToday")!
                }).ToList()
            }).ToList();

            return new PricingTrendResponse
            {
                ContractId = contractId,
                LoadType = (string?)trendEl.Attribute("loadType") ?? "All Hours",
                PriceUnit = (string?)trendEl.Attribute("priceUnit") ?? "$/MWh",
                Series = series
            };
        }

        public PricingSnapshotDetailsResponse? GetPricingSnapshotDetails(string contractId)
        {
            if (!File.Exists(_xmlFilePathPricingSnapshotDetails))
                throw new FileNotFoundException($"Pricing snapshot details file not found: {_xmlFilePathPricingSnapshotDetails}");

            var doc = XDocument.Load(_xmlFilePathPricingSnapshotDetails);

            var detailEl = doc.Root!.Elements("ContractSnapshotDetails")
                .FirstOrDefault(e => ((string)e.Attribute("contractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (detailEl is null)
                return null;

            var lowestEl = detailEl.Element("TodaysLowestPrice")!;

            return new PricingSnapshotDetailsResponse
            {
                ContractId = contractId,
                Date = (string)detailEl.Element("Date")!,
                PricingZone = (string)detailEl.Element("PricingZone")!,
                Product = (string)detailEl.Element("Product")!,
                TodaysLowestPrice = new TodaysLowestPrice
                {
                    TermMonths = (int)lowestEl.Element("TermMonths")!,
                    Price = (decimal)lowestEl.Element("Price")!,
                    PriceUnit = (string)lowestEl.Element("PriceUnit")!,
                    ChangeVsYesterday = (decimal)lowestEl.Element("ChangeVsYesterday")!,
                    ChangePercentVsYesterday = (decimal)lowestEl.Element("ChangePercentVsYesterday")!
                }
            };
        }

        public FormalQuoteResponse SubmitFormalQuote(FormalQuoteRequest request)
        {
            if (!File.Exists(_xmlFilePathFormalQuotes))
                throw new FileNotFoundException($"Formal quotes data file not found: {_xmlFilePathFormalQuotes}");

            var doc = XDocument.Load(_xmlFilePathFormalQuotes);
            var quotesEl = doc.Root!.Element("Quotes")!;

            int nextNumber = quotesEl.Elements("Quote").Select(e => (string)e.Element("QuoteId")!).Where(id => id.StartsWith("QT-", StringComparison.OrdinalIgnoreCase))
   .Select(id => int.TryParse(id["QT-".Length..], out var n) ? n : 0)
        .DefaultIfEmpty(0)
          .Max() + 1;

            string newQuoteId = $"QT-{nextNumber:D4}";
            string submittedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            quotesEl.AddFirst(new XElement("Quote",
            new XElement("QuoteId", newQuoteId),
            new XElement("ContractId", request.ContractId),
            new XElement("TermLength", request.TermLength),
            new XElement("Description", request.Description),
            new XElement("EstimatedAnnualUsage", request.EstimatedAnnualUsage),
            new XElement("SubmittedAt", submittedAt)
                ));

            doc.Save(_xmlFilePathFormalQuotes);

            string newCaseId = _supportCaseService.CreateCase(new SupportRequest
            {
                ContractId = request.ContractId,
                MeterId = string.Empty,
                RequestType = "Formal Quote Request",
                Priority = "MEDIUM",
                Subject = $"Formal Quote {newQuoteId}",
                Comment = $"Formal quote submitted: {request.Description}. Term: {request.TermLength} months, Estimated Annual Usage: {request.EstimatedAnnualUsage} kWh."
            });

            return new FormalQuoteResponse
            {
                QuoteId = newQuoteId,
                ContractId = request.ContractId,
                CaseId = newCaseId,
                TermLength = request.TermLength,
                Description = request.Description,
                EstimatedAnnualUsage = request.EstimatedAnnualUsage,
                SubmittedAt = submittedAt
            };
        }

        private List<XElement> LoadRenewalsXml()
        {
            if (!File.Exists(_xmlFilePathRenewals))
                throw new FileNotFoundException($"Renewals data file not found: {_xmlFilePathRenewals}");

            var doc = XDocument.Load(_xmlFilePathRenewals);
            return doc.Root!.Element("Contracts")!.Elements("Contract").ToList();
        }

        public RenewalTableResponse GetRenewalContracts()
        {
            var elements = LoadRenewalsXml();

            var rows = elements.Select(e => new RenewalTableRow
            {
                ContractId = (string)e.Element("ContractId")!,
                ServiceAddress = (string)e.Element("ServiceAddress")!,
                ContractStatus = (string)e.Element("ContractStatus")!,
                CurrentPlan = (int)e.Element("CurrentPlan")!,
                EndDate = (string)e.Element("EndDate")!,
                DaysRemaining = (int)e.Element("DaysRemaining")!,
                LastQuoteStatus = (string)e.Element("LastQuoteStatus")!
            }).ToList();

            return new RenewalTableResponse { Contracts = rows };
        }

        public ContractQuoteHistory? GetQuoteByContractId(string contractId)
        {
            var elements = LoadRenewalsXml();

            var contractEl = elements.FirstOrDefault(e =>
                ((string)e.Element("ContractId")!)
                .Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (contractEl is null) return null;

            var quotes = contractEl
                .Element("QuoteHistory")!
                .Elements("Quote")
                .Select(q => new QuoteEntry
                {
                    QuoteId = (string)q.Element("QuoteId")!,
                    RequestedOn = (string)q.Element("RequestedOn")!,
                    RequestedPlan = (string)q.Element("RequestedPlan")!,
                    TermLength = (int)q.Element("TermLength")!,
                    QuotedPrice = (decimal)q.Element("QuotedPrice")!,
                    Status = (string)q.Element("Status")!,
                    SentOn = (string?)q.Element("SentOn") ?? string.Empty
                })
                .ToList();

            return new ContractQuoteHistory
            {
                ContractId = (string)contractEl.Element("ContractId")!,
                Quotes = quotes
            };
        }

        public List<string> GetRenewalAddressDropdown()
        {
            return LoadRenewalsXml()
                .Select(e => (string)e.Element("ServiceAddress")!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();
        }

        public RenewalSummary GetRenewalSummary()
        {
            var contracts = LoadRenewalsXml();

            var activeContracts = contracts.Count(e =>
                ((string)e.Element("ContractStatus")!)
                .Equals("ACTIVE", StringComparison.OrdinalIgnoreCase));

            var expiringIn60Days = contracts.Count(e =>
                (int)e.Element("DaysRemaining")! <= 60);

            var allQuotes = contracts
                .SelectMany(e => e.Element("QuoteHistory")!.Elements("Quote"))
                .ToList();

            var openQuoteRequests = allQuotes.Count(q =>
            {
                var status = ((string)q.Element("Status")!).ToUpperInvariant();
                return status == "REQUESTED" || status == "QUOTE SENT";
            });

            var quotesApproved = allQuotes.Count(q =>
                ((string)q.Element("Status")!)
                .Equals("APPROVED", StringComparison.OrdinalIgnoreCase));

            return new RenewalSummary
            {
                ActiveContracts = activeContracts,
                ExpiringIn60Days = expiringIn60Days,
                OpenQuoteRequests = openQuoteRequests,
                QuotesApproved = quotesApproved
            };
        }

        public RenewalQuoteDetail? GetRenewalQuoteDetail(string quoteId)
        {
            var contractEls = LoadRenewalsXml();

            foreach (var contractEl in contractEls)
            {
                var quoteEl = contractEl
                    .Element("QuoteHistory")?
                    .Elements("Quote")
                    .FirstOrDefault(q =>
                        ((string)q.Element("QuoteId")!)
                        .Equals(quoteId, StringComparison.OrdinalIgnoreCase));

                if (quoteEl is null)
                    continue;

                return new RenewalQuoteDetail
                {
                    // Quote identity
                    QuoteId = (string)quoteEl.Element("QuoteId")!,
                    RequestedOn = (string)quoteEl.Element("RequestedOn")!,
                    // Contract context
                    CompanyName = (string)contractEl.Element("CompanyName")!,
                    ContractId = (string)contractEl.Element("ContractId")!,
                    ServiceAddress = (string)contractEl.Element("ServiceAddress")!,
                    ProductType = (string)contractEl.Element("ProductType")!,
                    ContractTerm = (string)contractEl.Element("ContactTerm")!,
                    MeterCount = (int)contractEl.Element("MeterCount")!,
                    StartDate = (string)contractEl.Element("StartDate")!,
                    EndDate = (string)contractEl.Element("EndDate")!,
                    DaysRemaining = (int)contractEl.Element("DaysRemaining")!,
                    // Quote pricing
                    PlanTermMonths = (int)quoteEl.Element("TermLength")!,
                    QuotedPrice = (decimal)quoteEl.Element("QuotedPrice")!,
                    RateType = (string)quoteEl.Element("RateType")!,
                    BaseCharge = (decimal)quoteEl.Element("BaseCharge")!,
                    BaseChargeUnit = (string?)quoteEl.Element("BaseChargeUnit") ?? string.Empty,
                    BrokerMargin = (decimal)quoteEl.Element("BrokerMargin")!,
                    FinalRate = (decimal)quoteEl.Element("FinalRate")!,
                    QuoteValidTill = (string?)quoteEl.Element("QuoteValidTill") ?? string.Empty
                };
            }

            return null;
        }

        public RenewContractResponse RequestContractRenewal(RenewContractRequest request)
        {
            if (!File.Exists(_xmlFilePathContractsTable))
                throw new FileNotFoundException($"Contract data file not found: {_xmlFilePathContractsTable}");

            var doc = XDocument.Load(_xmlFilePathContractsTable);
            var contractEls = doc.Root!.Element("Contracts")!.Elements("Contract").ToList();

            var updated = new List<RenewContractResult>();
            var notFound = new List<string>();

            foreach (var contractId in request.ContractIds)
            {
                var el = contractEls.FirstOrDefault(e => ((string)e.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

                if (el is null)
                {
                    notFound.Add(contractId);
                    continue;
                }

                el.Element("Status")!.Value = "PENDING";

                updated.Add(new RenewContractResult
                {
                    ContractId = contractId,
                    Status = "PENDING"
                });
            }

            if (updated.Count > 0)
                doc.Save(_xmlFilePathContractsTable);

            return new RenewContractResponse
            {
                Updated = updated,
                NotFound = notFound
            };
        }

        public ContractInfoResponse? GetContractInfo(string contractId)
        {
            var tableEls = LoadContractsTableXml();
            var tableEl = tableEls.FirstOrDefault(e =>
           ((string)e.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (tableEl is null)
                return null;

            string contractTerm = string.Empty;
            int daysRemaining = 0;

            if (File.Exists(_xmlFilePathRenewals))
            {
                var renewalEl = XDocument.Load(_xmlFilePathRenewals).Root!.Element("Contracts")!.Elements("Contract").FirstOrDefault(e => ((string)e.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

                if (renewalEl is not null)
                {
                    contractTerm = (string?)renewalEl.Element("ContactTerm") ?? string.Empty;
                    daysRemaining = (int?)renewalEl.Element("DaysRemaining") ?? 0;
                }
            }

            int numberOfMeters = 0;
            var metersFilePath = Path.Combine(Path.GetDirectoryName(_xmlFilePath)!, "contracts-meters.xml");
            if (File.Exists(metersFilePath))
            {
                var contractMeterEl = XDocument.Load(metersFilePath).Root!.Element("ContractMeters")!.Elements("ContractMeter").FirstOrDefault(e => ((string)e.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

                if (contractMeterEl is not null)
                    numberOfMeters = contractMeterEl.Element("Meters")!.Elements("Meter").Count();
            }

            return new ContractInfoResponse
            {
                ContractId = (string)tableEl.Element("ContractId")!,
                ContractStatus = (string)tableEl.Element("Status")!,
                ProductType = (string)tableEl.Element("ContractType")!,
                ContractTerm = contractTerm,
                StartDate = (string)tableEl.Element("StartDate")!,
                EndDate = (string)tableEl.Element("EndDate")!,
                DaysRemaining = daysRemaining,
                Utility = "--",
                NumberOfMeters = numberOfMeters
            };
        }
    }
}
