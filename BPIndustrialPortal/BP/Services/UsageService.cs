using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class UsageService : IUsageService
    {
        private readonly string _xmlFilePath;
        private readonly string _contractUsageXmlFilePath;
        private readonly string _usageTrendXmlFilePath;

        public UsageService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "usage.xml");
            _contractUsageXmlFilePath = Path.Combine(env.ContentRootPath, "Data", "contract-usage.xml");
            _usageTrendXmlFilePath = Path.Combine(env.ContentRootPath, "Data", "usage-trend.xml");
        }

        private (DateTime lastUpdated, List<XElement> elements) LoadXml()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Meter data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            var lastUpdated = DateTime.Parse((string)doc.Root!.Element("LastUpdated")!);
            var elements = doc.Root.Element("Meters")!.Elements("Meter").ToList();
            return (lastUpdated, elements);
        }

        private static MeterSummary MapToSummary(XElement e, DateTime lastUpdated)
        {
            var cmu = e.Element("CurrentMonthUsage")!;
            var uvc = e.Element("UsageVsContracted")!;
            var pd = e.Element("PeakDemand")!;
            var yoy = e.Element("YoYChange")!;

            return new MeterSummary
            {
                MeterId = (string)e.Element("MeterId")!,
                Label = (string)e.Element("Label")!,
                LastUpdated = lastUpdated,
                CurrentMonthUsage = new CurrentMonthUsage
                {
                    UsageMWh = (decimal)cmu.Element("UsageMWh")!,
                    VsLastMonthPercent = (decimal)cmu.Element("VsLastMonthPercent")!,
                    VsLastMonthDirection = (string?)cmu.Element("VsLastMonthDirection") ?? "up"
                },
                UsageVsContracted = new UsageVsContracted
                {
                    UtilizedPercent = (decimal)uvc.Element("UtilizedPercent")!,
                    UsedMWh = (decimal)uvc.Element("UsedMWh")!,
                    ContractedMWh = (decimal)uvc.Element("ContractedMWh")!
                },
                PeakDemand = new PeakDemand
                {
                    PeakKW = (decimal)pd.Element("PeakKW")!,
                    PeakDate = (string?)pd.Element("PeakDate") ?? string.Empty
                },
                YoYChange = new YoYChange
                {
                    ChangePercent = (decimal)yoy.Element("ChangePercent")!,
                    Direction = (string?)yoy.Element("Direction") ?? "up",
                    ComparedMWh = (decimal)yoy.Element("ComparedMWh")!,
                    ComparedYear = (int)yoy.Element("ComparedYear")!
                }
            };
        }

        public MeterDropdownResponse GetMeterDropdown()
        {
            var (lastUpdated, elements) = LoadXml();

            return new MeterDropdownResponse
            {
                LastUpdated = lastUpdated,
                Meters = elements
                .Select(e => new MeterDropdownItem
                {
                    MeterId = (string)e.Element("MeterId")!,
                    Label = (string)e.Element("Label")!
                })
                .ToList()
            };
        }

        public MeterSummary? GetMeterSummary(string meterId)
        {
            var (lastUpdated, elements) = LoadXml();

            var element = elements.FirstOrDefault(e =>
                        ((string)e.Element("MeterId")!).Equals(meterId, StringComparison.OrdinalIgnoreCase));

            return element is null ? null : MapToSummary(element, lastUpdated);
        }

        private (DateTime lastUpdated, List<XElement> contracts) LoadContractUsageXml()
        {
            if (!File.Exists(_contractUsageXmlFilePath))
                throw new FileNotFoundException($"Contract usage data file not found: {_contractUsageXmlFilePath}");

            var doc = XDocument.Load(_contractUsageXmlFilePath);
            var lastUpdated = DateTime.Parse((string)doc.Root!.Element("LastUpdated")!);
            var contracts = doc.Root.Element("Contracts")!.Elements("Contract").ToList();
            return (lastUpdated, contracts);
        }

        private static XElement? FindContract(List<XElement> contracts, string contractId) =>
          contracts.FirstOrDefault(c =>
              ((string)c.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

        private static ContractMeterSummary MapToContractMeterSummary(
                string contractId, XElement m, DateTime lastUpdated)
        {
            var tu = m.Element("TotalUsage")!;
            var cmu = m.Element("CurrentMonthUsage")!;
            var pd = m.Element("PeakDemand")!;
            var uv = m.Element("UsageVariance")!;

            return new ContractMeterSummary
            {
                ContractId = contractId,
                MeterId = (string)m.Element("MeterId")!,
                Label = (string)m.Element("Label")!,
                LastUpdated = lastUpdated,
                TotalUsage = new TotalUsageSummary
                {
                    TotalMWh = (decimal)tu.Element("TotalMWh")!,
                    VsLastMonthPercent = (decimal)tu.Element("VsLastMonthPercent")!,
                    VsLastMonthDirection = (string?)tu.Element("VsLastMonthDirection") ?? "up"
                },
                CurrentMonthUsage = new ContractCurrentMonthUsage
                {
                    UsageMWh = (decimal)cmu.Element("UsageMWh")!,
                    ContractedMWh = (decimal)cmu.Element("ContractedMWh")!
                },
                PeakDemand = new PeakDemand
                {
                    PeakKW = (decimal)pd.Element("PeakKW")!,
                    PeakDate = (string?)pd.Element("PeakDate") ?? string.Empty
                },
                UsageVariance = new UsageVariance
                {
                    ChangePercent = (decimal)uv.Element("ChangePercent")!,
                    Direction = (string?)uv.Element("Direction") ?? "up",
                    ComparedMWh = (decimal)uv.Element("ComparedMWh")!,
                    ComparedYear = (int)uv.Element("ComparedYear")!
                }
            };
        }


        public ContractUsageMeterDropdownResponse GetContractMeterDropdown(string contractId)
        {
            var (lastUpdated, contracts) = LoadContractUsageXml();
            var contract = FindContract(contracts, contractId);

            if (contract is null)
                return new ContractUsageMeterDropdownResponse
                {
                    ContractId = contractId,
                    LastUpdated = lastUpdated,
                    Meters = []
                };

            var meters = contract.Element("Meters")!.Elements("Meter")
                              .Select(m => new ContractUsageMeterItem
                              {
                                  MeterId = (string)m.Element("MeterId")!,
                                  Label = (string)m.Element("Label")!
                              }).ToList();

            return new ContractUsageMeterDropdownResponse
            {
                ContractId = contractId,
                LastUpdated = lastUpdated,
                Meters = meters
            };
        }

        public ContractMeterSummary? GetContractMeterSummary(string contractId, string meterId)
        {
            var (lastUpdated, contracts) = LoadContractUsageXml();
            var contract = FindContract(contracts, contractId);

            if (contract is null) return null;

            var meter = contract.Element("Meters")!.Elements("Meter")
                        .FirstOrDefault(m =>
                 ((string)m.Element("MeterId")!).Equals(meterId, StringComparison.OrdinalIgnoreCase));

            return meter is null ? null : MapToContractMeterSummary(contractId, meter, lastUpdated);
        }

        public List<MeterLabel> GetMeterIdsByContractId(string contractId)
        {
            var dropdown = GetContractMeterDropdown(contractId);
            return dropdown.Meters.Select(m => new MeterLabel { MeterId = m.MeterId, Label = m.Label }).ToList();
        }

        private List<XElement> LoadUsageTrendXml()
        {
            if (!File.Exists(_usageTrendXmlFilePath))
                throw new FileNotFoundException($"Usage trend data file not found: {_usageTrendXmlFilePath}");

            var doc = XDocument.Load(_usageTrendXmlFilePath);
            return doc.Root!.Element("Contracts")!.Elements("Contract").ToList();
        }

        public UsageTrendResponse? GetUsageTrend(string contractId, string meterId, DateTime startDate, DateTime endDate)
        {
            var contracts = LoadUsageTrendXml();

            var contract = contracts.FirstOrDefault(c =>((string)c.Element("ContractId")!).Equals(contractId, StringComparison.OrdinalIgnoreCase));

            if (contract is null) return null;

            var meter = contract.Element("Meters")!.Elements("Meter").FirstOrDefault(m =>
            ((string)m.Element("MeterId")!).Equals(meterId, StringComparison.OrdinalIgnoreCase));

            if (meter is null) return null;

            var monthKey = $"{startDate:yyyy-MM}";

            var trend = meter.Element("Trends")!.Elements("Trend")
            .FirstOrDefault(t =>
           ((string)t.Element("Month")!).Equals(monthKey, StringComparison.OrdinalIgnoreCase));

            if (trend is null) return null;

            var summaryEl = trend.Element("Summary")!;
            var summary = new UsageTrendSummary
            {
                TotalUsageMWh = (decimal)summaryEl.Element("TotalUsageMWh")!,
                ContractedBlockedVolumeMWh = (decimal)summaryEl.Element("ContractedBlockedVolumeMWh")!,
                MarketIndexVolumeMWh = (decimal)summaryEl.Element("MarketIndexVolumeMWh")!,
                ContractCoveragePercent = (decimal)summaryEl.Element("ContractCoveragePercent")!
            };

            var dataPoints = trend.Element("DataPoints")!.Elements("DataPoint")
               .Select(dp => new UsageTrendDataPoint
               {
                   Day = (int)dp.Element("Day")!,
                   ActualUsageMWh = (decimal)dp.Element("ActualUsageMWh")!,
                   ContractedBlockedVolumeMWh = (decimal)dp.Element("ContractedBlockedVolumeMWh")!,
                   MarketIndexVolumeMWh = (decimal)dp.Element("MarketIndexVolumeMWh")!,
                   AverageTemperatureF = (decimal)dp.Element("AverageTemperatureF")!
               })
              .Where(dp => dp.Day >= startDate.Day && dp.Day <= endDate.Day)
          .ToList();

            return new UsageTrendResponse
            {
                ContractId = contractId,
                MeterId = meterId,
                StartDate = startDate,
                EndDate = endDate,
                DataPoints = dataPoints,
                Summary = summary
            };
        }
    }
}
