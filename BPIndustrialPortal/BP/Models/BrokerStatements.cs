using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    // ── Statement List ─────────────────────────────────────────────────────────

    public class StatementListItem
    {
        public string StatementId { get; set; } = string.Empty;
        public string AgencyName { get; set; } = string.Empty;
        public string SubmittedOn { get; set; } = string.Empty;
        public decimal Payments { get; set; }
        public decimal Adjustments { get; set; }
        public decimal Clawbacks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
    }

    public class StatementListResponse
    {
        public int TotalEntries { get; set; }
        public List<StatementListItem> Statements { get; set; } = [];
    }

    // ── Statement Detail ───────────────────────────────────────────────────────

    public class StatementTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string UtilityNumber { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string BillDate { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Usage { get; set; }
        public decimal ChannelFee { get; set; }
        public decimal BrokerFee { get; set; }
        public decimal UnitFee { get; set; }
        public decimal Commission { get; set; }
        public string CommodityType { get; set; } = string.Empty;
    }

    public class StatementAgencyInfo
    {
        public string AgencyId { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string StatementEmail { get; set; } = string.Empty;
    }

    public class StatementSummaryInfo
    {
        public string StatementId { get; set; } = string.Empty;
        public decimal TotalClawbacks { get; set; }
        public decimal TotalWriteOffAmount { get; set; }
        public string StatementDate { get; set; } = string.Empty;
        public decimal TotalAmountOwed { get; set; }
        public decimal TotalAdjustments { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class StatementDetailResponse
    {
        public StatementAgencyInfo AgencyInfo { get; set; } = new();
        public StatementSummaryInfo StatementSummary { get; set; } = new();
        public List<StatementTransaction> ForecastedTransactions { get; set; } = [];
        public List<StatementTransaction> CurrentTransactions { get; set; } = [];
    }

    // ── Request / Response models ──────────────────────────────────────────────

    public class ApproveStatementRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "StatementId must not be empty.")]
        public string StatementId { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public List<string> IncludedTransactionIds { get; set; } = [];
    }

    public class RejectStatementRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "StatementId must not be empty.")]
        public string StatementId { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class BulkStatementRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one statement ID must be provided.")]
        public List<string> StatementIds { get; set; } = [];
    }

    public class EmailStatementRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "StatementId must not be empty.")]
        public string StatementId { get; set; } = string.Empty;
    }

    public class StatementActionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── Statements Summary ─────────────────────────────────────────────────────

    public class StatementsSummaryResponse
    {
        /// <summary>Total number of statements for the agency (all statuses).</summary>
        public int StatementsSubmitted { get; set; }

        /// <summary>Number of statements currently in PENDING status.</summary>
        public int PendingCount { get; set; }

        /// <summary>Badge label shown under Statements Submitted, e.g. "6 pending".</summary>
        public string PendingLabel { get; set; } = string.Empty;

        /// <summary>Sum of the Total field across all APPROVED statements.</summary>
        public decimal ApprovedCommission { get; set; }

        /// <summary>Descriptive label shown under the Approved Commission amount, e.g. "Approved statements total".</summary>
        public string ApprovedCommissionLabel { get; set; } = string.Empty;

        /// <summary>Number of statements in APPROVED status (ready for payout).</summary>
        public int ApprovedStatements { get; set; }

        /// <summary>Status label shown under Approved Statements, e.g. "Ready for payout".</summary>
        public string ApprovedStatementsLabel { get; set; } = string.Empty;
    }
}
