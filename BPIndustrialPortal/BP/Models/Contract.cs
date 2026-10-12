using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    // ?? Dropdown ??????????????????????????????????????????
    public class ContractDropdownItem
    {
        public string ContractId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ContractDropdownResponse
    {
        public DateTime LastUpdated { get; set; }
        public List<ContractDropdownItem> Contracts { get; set; } = [];
    }

    // ?? Summary data-point models ?????????????????????????

    /// <summary>Active contract count (card 1 � visible on "All" view)</summary>
    public class ActiveContractsInfo
    {
        public int Count { get; set; }
    }

    /// <summary>Total meter count (card 2)</summary>
    public class TotalMeterInfo
    {
        public int Count { get; set; }
        public int ActiveMeterCount { get; set; }   // badge: "Active meter"
    }

    /// <summary>Total contract volume (card 3)</summary>
    public class TotalContractVolumeInfo
    {
        public decimal VolumeMWh { get; set; }
        public decimal ContractedMWh { get; set; }  // badge: "X MWh contracted"
    }

    /// <summary>Expiring in 30 days (card 4 � visible on "All" view)</summary>
    public class ExpiringIn30DaysInfo
    {
        public int Count { get; set; }
    }

    /// <summary>Expiring contract (card 5 � visible on "All" view)</summary>
    public class ExpiringContractInfo
    {
        public int Count { get; set; }
        public string DaysLabel { get; set; } = string.Empty;  // e.g. "60 days"
    }

    /// <summary>Contract utilisation (card 1 � visible on individual contract view)</summary>
    public class ContractUtilizationInfo
    {
        public int Count { get; set; }
        public decimal UsedMWh { get; set; }        // badge: "X MWh used"
        public decimal ContractedMWh { get; set; }  // badge: "X MWh contracted"
    }

    /// <summary>Expiring-in countdown (card 2 � visible on individual contract view)</summary>
    public class ExpiringInInfo
    {
        public int Days { get; set; }
        public string ExpiryDate { get; set; } = string.Empty;  // badge: "Aug 31, 2025"
    }

    public class OpenQuoteRequestsInfo
    {
        public int Count { get; set; }
    }

    public class QuotesApprovedInfo
    {
        public int Count { get; set; }
    }

    // ?? Contract table row (contracts-table.xml) ?????????
    public class ContractTableRow
    {
        public string ContractId { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public int MeterCount { get; set; }
        public int Amendments { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }

    public class ContractTableResponse
    {
        public List<ContractTableRow> Contracts { get; set; } = [];
    }

    // ?? Root summary response ?????????????????????????????
    public class ContractSummary
    {
        public string ContractId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }

        // Every field is always present; frontend controls which cards to show.
        public ActiveContractsInfo ActiveContracts { get; set; } = new();
        public TotalMeterInfo TotalMeter { get; set; } = new();
        public TotalContractVolumeInfo TotalContractVolume { get; set; } = new();
        public ExpiringIn30DaysInfo ExpiringIn30Days { get; set; } = new();
        public ExpiringContractInfo ExpiringContract { get; set; } = new();
        public ContractUtilizationInfo ContractUtilization { get; set; } = new();
        public ExpiringInInfo ExpiringIn { get; set; } = new();
        public OpenQuoteRequestsInfo OpenQuoteRequests { get; set; } = new();
        public QuotesApprovedInfo QuotesApproved { get; set; } = new();
    }

    public class ContractDocument
    {
        public int DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SignedDate { get; set; } = string.Empty;
    }

    public class ContractDetail
    {
        public string ContractId { get; set; } = string.Empty;
        public List<ContractDocument> Documents { get; set; } = [];
    }

    // ?? Contract renewal (contracts-renew.xml) ???????????
    public class RenewContractAddress
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class RenewContractPlan
    {
        public int TermMonths { get; set; }
        public string? Badge { get; set; }
        public decimal Rate { get; set; }
        public string RateUnit { get; set; } = string.Empty;
        public decimal UsageBasis { get; set; }
        public string RateType { get; set; } = string.Empty;
        public decimal BaseCharge { get; set; }
        public string BaseChargeUnit { get; set; } = string.Empty;
    }

    public class RenewContractsResponse
    {
        public List<RenewContractAddress> ServiceAddresses { get; set; } = [];
        public List<string> ExpiringContractIds { get; set; } = [];
        public List<RenewContractPlan> Plans { get; set; } = [];
    }

    // ?? Renewals page table (renewals.xml) ???????????????
    public class RenewalTableRow
    {
        public string ContractId { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public int CurrentPlan { get; set; }
        public string EndDate { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string LastQuoteStatus { get; set; } = string.Empty;
    }

    public class RenewalTableResponse
    {
        public List<RenewalTableRow> Contracts { get; set; } = [];
    }

    public class QuoteEntry
    {
        public string QuoteId { get; set; } = string.Empty;
        public string RequestedOn { get; set; } = string.Empty;
        public string RequestedPlan { get; set; } = string.Empty;
        public int TermLength { get; set; }
        public decimal QuotedPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SentOn { get; set; } = string.Empty;
    }

    public class ContractQuoteHistory
    {
        public string ContractId { get; set; } = string.Empty;
        public List<QuoteEntry> Quotes { get; set; } = [];
    }

    // ?? Renewals summary cards (api/GetRenewalSummary) ??????
    public class RenewalSummary
    {
        public int ActiveContracts { get; set; }
        public int ExpiringIn60Days { get; set; }
        public int OpenQuoteRequests { get; set; }
        public int QuotesApproved { get; set; }
    }

    // ?? Renewal quote popup (api/GetRenewalQuoteDetail) ?????
    public class RenewalQuoteDetail
    {
        public string QuoteId { get; set; } = string.Empty;
        public string RequestedOn { get; set; } = string.Empty;
        // Contract context
        public string CompanyName { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string ContractTerm { get; set; } = string.Empty;
        public int MeterCount { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        // Quote pricing
        public int PlanTermMonths { get; set; }
        public decimal QuotedPrice { get; set; }
        public string RateType { get; set; } = string.Empty;
        public decimal BaseCharge { get; set; }
        public string BaseChargeUnit { get; set; } = string.Empty;
        public decimal BrokerMargin { get; set; }
        public decimal FinalRate { get; set; }
        public string QuoteValidTill { get; set; } = string.Empty;
    }

    public class RenewContractRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one contract ID is required.")]
        public List<string> ContractIds { get; set; } = [];
    }

    public class RenewContractResult
    {
        public string ContractId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class RenewContractResponse
    {
        public List<RenewContractResult> Updated { get; set; } = [];
        public List<string> NotFound { get; set; } = [];
    }

    /// <summary>Contract Information tab — sourced from contracts-table.xml + renewals.xml</summary>
    public class ContractInfoResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string ContractTerm { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string Utility { get; set; } = "--";
        public int NumberOfMeters { get; set; }
  }
}
