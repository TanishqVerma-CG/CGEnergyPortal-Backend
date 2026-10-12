using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BrokerCustomerService : IBrokerCustomerService
    {
        private readonly string _xmlFilePath;
        private readonly string _accountXmlFilePath;
        private readonly string _trendXmlFilePath;

        public BrokerCustomerService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "BrokerCustomer.xml");
            _accountXmlFilePath = Path.Combine(env.ContentRootPath, "Data", "BrokerCustomerAccount.xml");
            _trendXmlFilePath = Path.Combine(env.ContentRootPath, "Data", "CustomerMeterTrend.xml");
        }

        private XDocument Load()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker customer data file not found: {_xmlFilePath}");
            return XDocument.Load(_xmlFilePath);
        }

        private XDocument LoadAccount()
        {
            if (!File.Exists(_accountXmlFilePath))
                throw new FileNotFoundException($"Broker customer account data file not found: {_accountXmlFilePath}");
            return XDocument.Load(_accountXmlFilePath);
        }

        private XDocument LoadTrend()
        {
            if (!File.Exists(_trendXmlFilePath))
                throw new FileNotFoundException($"Customer meter trend data file not found: {_trendXmlFilePath}");
            return XDocument.Load(_trendXmlFilePath);
        }

        private XElement GetCustomerElement(int customerId)
        {
            var customer = LoadAccount().Root!.Elements("Customer").FirstOrDefault(c => (int?)c.Attribute("Id") == customerId);

            if (customer is null)
                throw new KeyNotFoundException($"Customer with Id '{customerId}' not found.");

            return customer;
        }

        public BrokerSummaryCards GetSummaryCards()
        {
            var root = Load().Root!;
            var sc = root.Element("SummaryCards")!;
            return new BrokerSummaryCards
            {
                TotalCustomers = (int?)sc.Element("TotalCustomers") ?? 0,
                ActiveContracts = (int?)sc.Element("ActiveContracts") ?? 0,
                MeterCount = (int?)sc.Element("MeterCount") ?? 0,
                TotalAnnualLoad = (double?)sc.Element("TotalAnnualLoad") ?? 0,
                TotalAnnualLoadUnit = (string?)sc.Element("TotalAnnualLoad")?.Attribute("unit") ?? string.Empty,
                ExpiringIn90Days = (int?)sc.Element("ExpiringIn90Days") ?? 0
            };
        }

        public BrokerCustomerPortfolioResponse GetCustomerPortfolio(int? page = null, int? pageSize = null, string? search = null)
        {
            var root = Load().Root!;
            var allCustomers = root.Element("CustomerPortfolio")!.Elements("Customer")
                .Select(MapCustomer)
                .Where(customer => string.IsNullOrWhiteSpace(search) ||
                    customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(customer => customer.Name)
                .ThenBy(customer => customer.Id)
                .ToList();

            var requestedPageSize = pageSize.HasValue
                ? Math.Clamp(pageSize.Value, 1, 100)
                : Math.Max(1, allCustomers.Count);
            var requestedPage = Math.Max(1, page ?? 1);
            var totalPages = Math.Max(1, (int)Math.Ceiling(allCustomers.Count / (double)requestedPageSize));
            requestedPage = Math.Min(requestedPage, totalPages);

            return new BrokerCustomerPortfolioResponse
            {
                TotalEntries = allCustomers.Count,
                Customers = allCustomers
                    .Skip((requestedPage - 1) * Math.Max(1, requestedPageSize))
                    .Take(Math.Max(1, requestedPageSize))
                    .ToList(),
                CurrentPage = requestedPage,
                PageSize = requestedPageSize,
                TotalPages = totalPages
            };
        }

        private static BrokerCustomer MapCustomer(XElement e) => new()
        {
            Id = (int?)e.Element("Id") ?? 0,
            Name = (string?)e.Element("Name") ?? string.Empty,
            ActiveContracts = (int?)e.Element("ActiveContracts") ?? 0,
            ActiveMeters = (int?)e.Element("ActiveMeters") ?? 0,
            AnnualUsage = (double?)e.Element("AnnualUsage") ?? 0,
            AnnualUsageUnit = (string?)e.Element("AnnualUsage")?.Attribute("unit") ?? string.Empty,
            ExpiringContracts = (int?)e.Element("ExpiringContracts") ?? 0,
            ExpiredContracts = (int?)e.Element("ExpiredContracts") ?? 0
        };

        public List<BrokerContract> GetCustomerContract(int customerId, string contractId)
        {
            var customer = MapContracts(GetCustomerElement(customerId));
            return customer.Where(item => item.ContractId.Equals(contractId, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        public BrokerMeterListResponse GetCustomerMeters(int customerId)
        {
            return PageMeters(GetCustomerElement(customerId));
        }

        private static List<BrokerContract> MapContracts(XElement customer) =>
            customer.Elements("Contract").Select(c => new BrokerContract
            {
                ContractId = (string?)c.Element("ContractId") ?? string.Empty,
                Status = (string?)c.Element("Status") ?? string.Empty,
                MeterCount = (int?)c.Element("MeterCount") ?? 0,
                AnnualUsage = (double?)c.Element("AnnualUsage") ?? 0,
                AnnualUsageUnit = (string?)c.Element("AnnualUsage")?.Attribute("Unit") ?? string.Empty,
                ContractTermMonths = (int?)c.Element("ContractTermMonths") ?? 0,
                BrokerMargin = (double?)c.Element("BrokerMargin") ?? 0,
                Price = (double?)c.Element("Price") ?? 0,
                Product = (string?)c.Element("Product") ?? string.Empty,
                StartDate = (string?)c.Element("StartDate") ?? string.Empty,
                EndDate = (string?)c.Element("EndDate") ?? string.Empty
            }).ToList();

        private static BrokerMeterListResponse PageMeters(XElement customer)
        {
            var allMeters = customer.Element("Meters")?.Elements("Meter")
                .Select(m => new BrokerMeter
                {
                    MeterId = (string?)m.Element("MeterId") ?? string.Empty,
                    UAN = (string?)m.Element("UAN") ?? string.Empty,
                    LDC = (string?)m.Element("LDC") ?? string.Empty,
                    Utility = (string?)m.Element("Utility") ?? string.Empty,
                    ServiceAddress = (string?)m.Element("ServiceAddress") ?? string.Empty,
                    Status = (string?)m.Element("Status") ?? string.Empty,
                    StartDate = (string?)m.Element("StartDate") ?? string.Empty,
                    StopDate = (string?)m.Element("StopDate") ?? string.Empty,
                    AnnualUsage = (double?)m.Element("AnnualUsage") ?? 0,
                    AnnualUsageUnit = (string?)m.Element("AnnualUsage")?.Attribute("Unit") ?? string.Empty
                }).ToList() ?? [];



            int totalEntries = allMeters.Count;

            return new BrokerMeterListResponse
            {
                TotalEntries = totalEntries,
                Meters = allMeters.ToList()
            };
        }

        public List<string> GetCustomerContractIds(int customerId)
        {
            var customerEl = GetCustomerElement(customerId);
            var ids = customerEl.Elements("Contract")
                .Select(c => (string?)c.Element("ContractId") ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();
            return ids;
        }

        public List<string> GetCustomerMeterIds(int customerId)
        {
            var customerEl = GetCustomerElement(customerId);
            var ids = customerEl.Element("Meters")?.Elements("Meter")
                .Select(m => (string?)m.Element("MeterId") ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList() ?? new List<string>();
            return ids;
        }

        public BrokerUsageTrendResponse GetBrokerUsageTrend(string meterId, DateTime startDate, DateTime endDate)
        {
            var root = LoadTrend().Root!;

            var meterElement = root.Elements("Meter").FirstOrDefault(m => (string?)m.Element("MeterId") == meterId);

            if (meterElement is null)
                return null!;

            var trends = meterElement.Element("Trends")?.Elements("Trend")
                .Select(t =>
                {
                    var monthStr = (string?)t.Element("Month") ?? string.Empty;
                    if (!DateTime.TryParseExact(monthStr + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var monthDate))
                        return null;

                    var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    if (monthEnd < startDate || monthStart > endDate)
                        return null;

                    var summary = t.Element("Summary");
                    var dataPoints = t.Element("DataPoints")?.Elements("DataPoint")
                   .Select(dp =>
                   {
                       var day = (int?)dp.Element("Day") ?? 0;
                       var date = new DateTime(monthDate.Year, monthDate.Month, day);
                       if (date < startDate || date > endDate)
                           return null;
                       return new BrokerUsageTrendDataPoint
                       {
                           Day = day,
                           ActualUsageMWh = (decimal?)dp.Element("ActualUsageMWh") ?? 0,
                           ContractedBlockedVolumeMWh = (decimal?)dp.Element("ContractedBlockedVolumeMWh") ?? 0,
                           MarketIndexVolumeMWh = (decimal?)dp.Element("MarketIndexVolumeMWh") ?? 0,
                           AverageTemperatureF = (decimal?)dp.Element("AverageTemperatureF") ?? 0
                       };
                   }).Where(dp => dp is not null).Cast<BrokerUsageTrendDataPoint>().ToList() ?? [];

                    return new BrokerUsageTrendMonth
                    {
                        Month = monthStr,
                        Summary = new BrokerUsageTrendSummary
                        {
                            TotalUsageMWh = (decimal?)summary?.Element("TotalUsageMWh") ?? 0,
                            ContractedBlockedVolumeMWh = (decimal?)summary?.Element("ContractedBlockedVolumeMWh") ?? 0,
                            MarketIndexVolumeMWh = (decimal?)summary?.Element("MarketIndexVolumeMWh") ?? 0,
                            ContractCoveragePercent = (decimal?)summary?.Element("ContractCoveragePercent") ?? 0
                        },
                        DataPoints = dataPoints
                    };
                }).Where(t => t is not null).Cast<BrokerUsageTrendMonth>().ToList() ?? [];

            return new BrokerUsageTrendResponse
            {
                MeterId = meterId,
                StartDate = startDate,
                EndDate = endDate,
                Trends = trends
            };
        }

        public List<BrokerCustomerDropdown> GetCustomerListForDropdown()
        {
            var root = Load().Root!;
            var customers = root.Element("CustomerPortfolio")?.Elements("Customer")
                .Select(c => new BrokerCustomerDropdown
                {
                    Id = (int?)c.Element("Id") ??0,
                    Name = (string?)c.Element("Name") ?? string.Empty
                }).ToList() ?? new List<BrokerCustomerDropdown>();

            return customers;
        }
    }
}
