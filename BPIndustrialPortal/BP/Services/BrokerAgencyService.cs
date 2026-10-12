using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BrokerAgencyService : IBrokerAgencyService
    {
        private readonly string _xmlFilePath;

        public BrokerAgencyService(IWebHostEnvironment env)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "broker-commission-agencies.xml");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private List<XElement> Load()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker commission agencies data file not found: {_xmlFilePath}");

            var doc = XDocument.Load(_xmlFilePath);
            return doc.Root!.Element("Agencies")!.Elements("Agency").ToList();
        }

        private static AgencyDashboardResponse MapDashboard(XElement a)
        {
            var address = a.Element("Address");

            var onFlowDeals = a.Element("OnFlowDeals")?
                .Elements("DataPoint")
                .Select(p => new OnFlowDealPoint
                {
                    Month = (string?)p.Element("Month") ?? string.Empty,
                    DealCount = int.Parse((string)p.Element("DealCount")!)
                })
                .ToList() ?? [];

            var paymentChart = a.Element("PaymentHistory")?
                .Elements("DataPoint")
                .Select(p => new PaymentChartPoint
                {
                    Month = (string?)p.Element("Month") ?? string.Empty,
                    Amount = decimal.Parse((string)p.Element("Amount")!),
                    IsForecasted = bool.Parse((string)p.Element("IsForecasted")!)
                })
                .ToList() ?? [];

            var agents = a.Element("Agents")?
                .Elements("Agent")
                .Select(ag => new AgentItem
                {
                    AgentId = (string?)ag.Element("AgentId") ?? string.Empty,
                    AgentName = (string?)ag.Element("AgentName") ?? string.Empty,
                    Role = (string?)ag.Element("Role") ?? string.Empty,
                    Email = (string?)ag.Element("Email") ?? string.Empty,
                    Phone = (string?)ag.Element("Phone") ?? string.Empty,
                    ActiveDeals = int.TryParse((string?)ag.Element("ActiveDeals"), out var ad) ? ad : 0
                })
                .ToList() ?? [];

            return new AgencyDashboardResponse
            {
                Agency = new AgencyDetail
                {
                    AgencyId = (string?)a.Element("AgencyId") ?? string.Empty,
                    Name = (string?)a.Element("AgencyName") ?? string.Empty,
                    Address = new AgencyAddress
                    {
                        Street = (string?)address?.Element("Street") ?? string.Empty,
                        City = (string?)address?.Element("City") ?? string.Empty,
                        State = (string?)address?.Element("State") ?? string.Empty,
                        Zip = (string?)address?.Element("Zip") ?? string.Empty
                    }
                },
                OnFlowDeals = onFlowDeals,
                PaymentChart = paymentChart,
                AgencySummary = new AgencySummaryMetrics
                {
                    NumberOfAgents = int.Parse((string)a.Element("NumberOfAgents")!),
                    NumberOfDeals = int.Parse((string)a.Element("NumberOfDeals")!),
                    CurrentPayment = decimal.Parse((string)a.Element("CurrentPayment")!),
                    CurrentPaymentPeriod = (string?)a.Element("CurrentPaymentPeriod") ?? string.Empty,
                    PreviousPayment = decimal.Parse((string)a.Element("PreviousPayment")!),
                    PreviousPaymentPeriod = (string?)a.Element("PreviousPaymentPeriod") ?? string.Empty,
                    ForecastedPayment = decimal.Parse((string)a.Element("ForecastedPayment")!),
                    YearToDatePayment = decimal.Parse((string)a.Element("YearToDatePayment")!)
                },
                Agents = agents
            };
        }

        // ── Public methods ─────────────────────────────────────────────────────

        public BrokerSearchResponse SearchBrokers(string? contactName, string? agencyName)
        {
            var agencies = Load();

            var filtered = agencies.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(contactName))
                filtered = filtered.Where(a =>
                    ((string?)a.Element("ContactName") ?? string.Empty)
                        .Contains(contactName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(agencyName))
                filtered = filtered.Where(a =>
                    ((string?)a.Element("AgencyName") ?? string.Empty)
                        .Contains(agencyName, StringComparison.OrdinalIgnoreCase));

            var results = filtered.Select(a => new BrokerSearchResult
            {
                AgencyId = (string?)a.Element("AgencyId") ?? string.Empty,
                AgencyName = (string?)a.Element("AgencyName") ?? string.Empty,
                ContactName = (string?)a.Element("ContactName") ?? string.Empty,
                NumberOfContracts = int.Parse((string)a.Element("NumberOfContracts")!)
            }).ToList();

            return new BrokerSearchResponse
            {
                TotalEntries = results.Count,
                Results = results
            };
        }

        public AgencyDashboardResponse? GetAgencyDashboard(string agencyId)
        {
            var agencies = Load();
            var agency = agencies.FirstOrDefault(a =>
                string.Equals((string?)a.Element("AgencyId"), agencyId, StringComparison.OrdinalIgnoreCase));

            return agency is null ? null : MapDashboard(agency);
        }
    }
}
