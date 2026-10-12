using System.ComponentModel.DataAnnotations;

namespace BP.Models
{

    public class BrokerRenewalsSummary
    {
        public int UpcomingRenewals { get; set; }
        public int CustomersImpacted { get; set; }
        public int RenewalDueThisWeek { get; set; }
        public int DueToday { get; set; }
        public int PendingRenewalRequests { get; set; }
        public int NewRequestsToday { get; set; }
        public int QuotesPending { get; set; }
        public int CustomersAwaitingQuotes { get; set; }
        public int TotalEntries { get; set; }

    }

    public class BrokerCustomerRenewalRow
    {
        public string CompanyName { get; set; } = string.Empty;
        public int TotalContracts { get; set; }
        public int ExpiringContracts { get; set; }
        public int PendingRequests { get; set; }
    }

    public class BrokerCustomerRenewalsListResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerCustomerRenewalRow> Customers { get; set; } = [];
    }

    // ── Broker Renewals – Customer Detail Page ─────────────────────────────────

    public class BrokerCustomerRenewalSummary
    {
        public int ActiveContracts { get; set; }
        public int RenewalPending { get; set; }
        public int OpenRequest { get; set; }
        public int QuotesApproved { get; set; }
    }

    public class BrokerRenewalContractRow
    {
        public string ContractId { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public int CurrentPlan { get; set; }
        public string EndDate { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string ContractTerm { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string MeterCount { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string LastQuoteStatus { get; set; } = string.Empty;
    }

    public class BrokerCustomerContractsResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerRenewalContractRow> Contracts { get; set; } = [];
    }

    public class BrokerContractDropdownOption
    {
        public string ContractId { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
    }

    public class BrokerRenewalQuoteEntry
    {
        public string QuoteId { get; set; } = string.Empty;
        public string RequestedOn { get; set; } = string.Empty;
        public string RequestedPlan { get; set; } = string.Empty;
        public int TermLength { get; set; }
        public decimal QuotedPrice { get; set; }
        public decimal BrokerMargin { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SentOn { get; set; } = string.Empty;
    }

    public class BrokerContractQuoteHistory
    {
        public string ContractId { get; set; } = string.Empty;
        public List<BrokerRenewalQuoteEntry> Quotes { get; set; } = [];
    }


    public class BrokerContractDetails
    {
        public string ContractId { get; set; } = string.Empty;
        public string ContractTerm { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string ServiceAddress { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string MeterCount { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public string LastQuoteStatus { get; set; } = string.Empty;
    }

    public class QuoteStatusRequest
    {
        [Required]
        public string QuoteId { get; set; } = string.Empty;
    }

    public class QuoteStatusResponse
    {
        public string QuoteId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
