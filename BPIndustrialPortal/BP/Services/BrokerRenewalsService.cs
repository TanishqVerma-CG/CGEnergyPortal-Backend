using System.Xml.Linq;
using BP.IService;
using BP.Models;

namespace BP.Services
{
    public class BrokerRenewalsService : IBrokerRenewalsService
    {
        private readonly string _xmlFilePath;
        private readonly IContractService _contractService;
        private const int ExpiringThresholdDays = 30;

        public BrokerRenewalsService(IWebHostEnvironment env, IContractService contractService)
        {
            _xmlFilePath = Path.Combine(env.ContentRootPath, "Data", "broker-renewals.xml");
            _contractService = contractService;
        }


        private XDocument LoadXml()
        {
            if (!File.Exists(_xmlFilePath))
                throw new FileNotFoundException($"Broker renewals data file not found: {_xmlFilePath}");
            return XDocument.Load(_xmlFilePath);
        }

        private List<XElement> GetAllCustomers()
        {
            var doc = LoadXml();
            return doc.Root!.Element("Customers")!.Elements("Customer").ToList();
        }

        private XElement FindCustomer(string companyName)
        {
            var customer = GetAllCustomers().FirstOrDefault(c =>
                ((string)c.Element("CompanyName")!)
                .Equals(companyName, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException($"Customer '{companyName}' not found.");
            return customer;
        }

        private static List<XElement> GetContracts(XElement customerEl) =>
            customerEl.Element("Contracts")!.Elements("Contract").ToList();

        private static bool IsPending(string lastQuoteStatus)
        {
            var s = lastQuoteStatus.ToUpperInvariant();
            return s == "REQUESTED" || s == "QUOTE SENT";
        }

        private static BrokerRenewalContractRow MapContractRow(XElement c) => new()
        {
            ContractId = (string)c.Element("ContractId")!,
            ServiceAddress = (string)c.Element("ServiceAddress")!,
            ContractStatus = (string)c.Element("ContractStatus")!,
            CurrentPlan = (int)c.Element("CurrentPlan")!,
            EndDate = (string)c.Element("EndDate")!,
            DaysRemaining = (int)c.Element("DaysRemaining")!,
            LastQuoteStatus = (string)c.Element("LastQuoteStatus")!,
            StartDate = (string)c.Element("StartDate")!,
            ContractTerm = (string)c.Element("ContractTerm")!,
            ProductType = (string)c.Element("ProductType")!,
            MeterCount = (string)c.Element("MeterCount")!
        };

        private static List<BrokerRenewalContractRow> ApplyContractSort(
            List<BrokerRenewalContractRow> rows, string sortBy, string sortOrder)
        {
            bool asc = !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "serviceaddress" => asc ? rows.OrderBy(r => r.ServiceAddress).ToList()
                                         : rows.OrderByDescending(r => r.ServiceAddress).ToList(),
                "contractstatus" => asc ? rows.OrderBy(r => r.ContractStatus).ToList()
                                         : rows.OrderByDescending(r => r.ContractStatus).ToList(),
                "currentplan" => asc ? rows.OrderBy(r => r.CurrentPlan).ToList()
                                         : rows.OrderByDescending(r => r.CurrentPlan).ToList(),
                "enddate" => asc ? rows.OrderBy(r => ParseDate(r.EndDate)).ToList()
                                         : rows.OrderByDescending(r => ParseDate(r.EndDate)).ToList(),
                "daysremaining" => asc ? rows.OrderBy(r => r.DaysRemaining).ToList()
                                         : rows.OrderByDescending(r => r.DaysRemaining).ToList(),
                "lastquotestatus" => asc ? rows.OrderBy(r => r.LastQuoteStatus).ToList()
                                         : rows.OrderByDescending(r => r.LastQuoteStatus).ToList(),
                _ => asc ? rows.OrderBy(r => r.ContractId).ToList()
                                         : rows.OrderByDescending(r => r.ContractId).ToList()
            };
        }

        private static List<BrokerCustomerRenewalRow> ApplyCustomerSort(
            List<BrokerCustomerRenewalRow> rows, string sortBy, string sortOrder)
        {
            bool asc = !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLower() switch
            {
                "totalcontracts" => asc ? rows.OrderBy(r => r.TotalContracts).ToList()
                                           : rows.OrderByDescending(r => r.TotalContracts).ToList(),
                "expiringcontracts" => asc ? rows.OrderBy(r => r.ExpiringContracts).ToList()
                                           : rows.OrderByDescending(r => r.ExpiringContracts).ToList(),
                "pendingrequests" => asc ? rows.OrderBy(r => r.PendingRequests).ToList()
                                           : rows.OrderByDescending(r => r.PendingRequests).ToList(),
                _ => asc ? rows.OrderBy(r => r.CompanyName).ToList()
                                           : rows.OrderByDescending(r => r.CompanyName).ToList()
            };
        }

        private static DateTime ParseDate(string value) =>
            DateTime.TryParse(value, out var d) ? d : DateTime.MinValue;


        public BrokerRenewalsSummary GetRenewalsSummary()
        {
            var doc = LoadXml();
            var s = doc.Root!.Element("Summary")!;

            return new BrokerRenewalsSummary
            {
                UpcomingRenewals = (int)s.Element("UpcomingRenewals")!,
                CustomersImpacted = (int)s.Element("CustomersImpacted")!,
                RenewalDueThisWeek = (int)s.Element("RenewalDueThisWeek")!,
                DueToday = (int)s.Element("DueToday")!,
                PendingRenewalRequests = (int)s.Element("PendingRenewalRequests")!,
                NewRequestsToday = (int)s.Element("NewRequestsToday")!,
                QuotesPending = (int)s.Element("QuotesPending")!,
                CustomersAwaitingQuotes = (int)s.Element("CustomersAwaitingQuotes")!,
                TotalEntries = (int)s.Element("TotalEntries")!
            };
        }

        public BrokerCustomerRenewalsListResponse GetCustomerRenewalsList(string sortBy, string sortOrder)
        {
            var customers = GetAllCustomers();

            var rows = customers.Select(c =>
            {
                var contracts = GetContracts(c);
                return new BrokerCustomerRenewalRow
                {
                    CompanyName = (string)c.Element("CompanyName")!,
                    TotalContracts = contracts.Count,
                    ExpiringContracts = contracts.Count(x => (int)x.Element("DaysRemaining")! <= ExpiringThresholdDays),
                    PendingRequests = contracts.Count(x => IsPending((string)x.Element("LastQuoteStatus")!))
                };
            }).ToList();

            rows = ApplyCustomerSort(rows, sortBy, sortOrder);

            return new BrokerCustomerRenewalsListResponse
            {
                TotalEntries = rows.Count,
                Customers = rows
            };
        }


        public BrokerCustomerRenewalSummary GetCustomerRenewalSummary(string companyName)
        {
            var contracts = GetContracts(FindCustomer(companyName));

            return new BrokerCustomerRenewalSummary
            {
                ActiveContracts = contracts.Count(c =>
                    ((string)c.Element("ContractStatus")!).Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)),
                RenewalPending = contracts.Count(c =>
                    ((string)c.Element("LastQuoteStatus")!).Equals("REQUESTED", StringComparison.OrdinalIgnoreCase)),
                OpenRequest = contracts.Count(c =>
                    ((string)c.Element("LastQuoteStatus")!).Equals("QUOTE SENT", StringComparison.OrdinalIgnoreCase)),
                QuotesApproved = contracts.Count(c =>
                    ((string)c.Element("LastQuoteStatus")!).Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            };
        }

        public BrokerCustomerContractsResponse GetCustomerContracts(
            string companyName,
            string serviceAddress,
            string expiringContract,
            string sortBy,
            string sortOrder)
        {
            var contracts = GetContracts(FindCustomer(companyName));

            if (!string.IsNullOrWhiteSpace(serviceAddress) &&
                !serviceAddress.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                contracts = contracts
                    .Where(c => ((string)c.Element("ServiceAddress")!)
                        .Equals(serviceAddress, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(expiringContract) &&
                !expiringContract.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                contracts = contracts
                    .Where(c => ((string)c.Element("ContractId")!)
                        .Equals(expiringContract, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var rows = contracts.Select(MapContractRow).ToList();
            rows = ApplyContractSort(rows, sortBy, sortOrder);

            return new BrokerCustomerContractsResponse
            {
                TotalEntries = rows.Count,
                Contracts = rows
            };
        }

        public List<string> GetServiceAddressDropdown(string companyName)
        {
            return GetContracts(FindCustomer(companyName))
                .Select(c => (string)c.Element("ServiceAddress")!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();
        }

        public List<BrokerContractDropdownOption> GetContractDropdown(string companyName)
        {
            return GetContracts(FindCustomer(companyName))
                .Select(c => new BrokerContractDropdownOption
                {
                    ContractId = (string)c.Element("ContractId")!,
                    ServiceAddress = (string)c.Element("ServiceAddress")!
                })
                .ToList();
        }

        public BrokerContractQuoteHistory? GetQuoteByContractId(string contractId)
        {
            foreach (var customer in GetAllCustomers())
            {
                var contractEl = GetContracts(customer).FirstOrDefault(c =>
                    ((string)c.Element("ContractId")!)
                    .Equals(contractId, StringComparison.OrdinalIgnoreCase));

                if (contractEl is null) continue;

                var quotes = contractEl
                    .Element("QuoteHistory")!
                    .Elements("Quote")
                    .Select(q => new BrokerRenewalQuoteEntry
                    {
                        QuoteId = (string)q.Element("QuoteId")!,
                        RequestedOn = (string)q.Element("RequestedOn")!,
                        RequestedPlan = (string)q.Element("RequestedPlan")!,
                        TermLength = (int)q.Element("TermLength")!,
                        QuotedPrice = (decimal)q.Element("QuotedPrice")!,
                        BrokerMargin = (decimal)q.Element("BrokerMargin")!,
                        Status = (string)q.Element("Status")!,
                        SentOn = (string?)q.Element("SentOn") ?? string.Empty
                    })
                    .ToList();

                return new BrokerContractQuoteHistory
                {
                    ContractId = contractId,
                    Quotes = quotes
                };
            }

            return null;
        }

        public BrokerContractDetails? GetContractDetails(string contractId)
        {
            foreach (var customer in GetAllCustomers())
            {
                var c = GetContracts(customer).FirstOrDefault(x =>
                    ((string)x.Element("ContractId")!)
                    .Equals(contractId, StringComparison.OrdinalIgnoreCase));

                if (c is null) continue;

                return new BrokerContractDetails
                {
                    ContractId = contractId,
                    ContractTerm = (string?)c.Element("ContractTerm") ?? string.Empty,
                    StartDate = (string?)c.Element("StartDate") ?? string.Empty,
                    EndDate = (string?)c.Element("EndDate") ?? string.Empty,
                    DaysRemaining = (int)c.Element("DaysRemaining")!,
                    ServiceAddress = (string?)c.Element("ServiceAddress") ?? string.Empty,
                    ProductType = (string?)c.Element("ProductType") ?? string.Empty,
                    MeterCount = (string?)c.Element("MeterCount") ?? string.Empty,
                    ContractStatus = (string?)c.Element("ContractStatus") ?? string.Empty,
                    LastQuoteStatus = (string?)c.Element("LastQuoteStatus") ?? string.Empty
                };
            }

            return null;
        }

        private QuoteStatusResponse UpdateQuoteStatus(string quoteId, string newStatus)
        {
            var doc = LoadXml();

            XElement? matchedQuote = null;
            XElement? matchedContract = null;

            foreach (var customer in doc.Root!.Element("Customers")!.Elements("Customer"))
            {
                foreach (var contract in customer.Element("Contracts")!.Elements("Contract"))
                {
                    var quote = contract.Element("QuoteHistory")?.Elements("Quote").FirstOrDefault(q =>((string)q.Element("QuoteId")!).Equals(quoteId, StringComparison.OrdinalIgnoreCase));

                    if (quote is null) continue;

                    matchedQuote = quote;
                    matchedContract = contract;
                    break;
                }
                if (matchedQuote is not null) break;
            }

            if (matchedQuote is null || matchedContract is null)
                throw new KeyNotFoundException($"Quote '{quoteId}' not found.");

            matchedQuote.Element("Status")!.Value = newStatus;
            var firstQuote = matchedContract.Element("QuoteHistory")!.Elements("Quote").FirstOrDefault();

            if (firstQuote is not null && ((string)firstQuote.Element("QuoteId")!).Equals(quoteId, StringComparison.OrdinalIgnoreCase))
            {
                matchedContract.Element("LastQuoteStatus")!.Value = newStatus;
            }

            doc.Save(_xmlFilePath);

            return new QuoteStatusResponse
            {
                QuoteId = quoteId,
                ContractId = (string)matchedContract.Element("ContractId")!,
                Status = newStatus,
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        public QuoteStatusResponse ApproveQuote(string quoteId) =>
            UpdateQuoteStatus(quoteId, "APPROVED");

        public QuoteStatusResponse RejectQuote(string quoteId) =>
            UpdateQuoteStatus(quoteId, "REJECTED");

        public RenewalQuoteDetail? GetRenewalQuoteDetail(string quoteId)
        {
            foreach (var customer in GetAllCustomers())
            {
                foreach (var contractEl in GetContracts(customer))
                {
                    var quoteEl = contractEl
                        .Element("QuoteHistory")?
                        .Elements("Quote")
                        .FirstOrDefault(q =>
                            ((string)q.Element("QuoteId")!)
                            .Equals(quoteId, StringComparison.OrdinalIgnoreCase));

                    if (quoteEl is null)
                        continue;

                    var contractTerm = (string?)contractEl.Element("ContractTerm") ?? string.Empty;

                    return new RenewalQuoteDetail
                    {
                        QuoteId = (string)quoteEl.Element("QuoteId")!,
                        RequestedOn = (string)quoteEl.Element("RequestedOn")!,
                        CompanyName = (string)customer.Element("CompanyName")!,
                        ContractId = (string)contractEl.Element("ContractId")!,
                        ServiceAddress = (string)contractEl.Element("ServiceAddress")!,
                        ProductType = (string)contractEl.Element("ProductType")!,
                        ContractTerm = string.IsNullOrEmpty(contractTerm) ? string.Empty : $"{contractTerm} Month",
                        MeterCount = (int)contractEl.Element("MeterCount")!,
                        StartDate = (string)contractEl.Element("StartDate")!,
                        EndDate = (string)contractEl.Element("EndDate")!,
                        DaysRemaining = (int)contractEl.Element("DaysRemaining")!,
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
            }

            return null;
        }

        public PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot, string priceUnit) {
            return _contractService.GetPricingIntelligence(contractId, snapshot, priceUnit);
        }

        public PricingTrendResponse? GetPricingTrend(string contractId) {
            return _contractService.GetPricingTrend(contractId);
        }

        public PricingSnapshotDetailsResponse? GetPricingSnapshotDetails(string contractId) {
            return _contractService.GetPricingSnapshotDetails(contractId);
        }
    }
}
