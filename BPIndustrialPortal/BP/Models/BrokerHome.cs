namespace BP.Models
{
    // ── Summary Metrics ────────────────────────────────────────────────────────

    public class BrokerCustomersManaged
    {
        public int Total { get; set; }
        public int NewThisMonth { get; set; }
        public string NewThisMonthLabel { get; set; } = string.Empty;
        public int Churned { get; set; }
        public string ChurnedLabel { get; set; } = string.Empty;
    }

    public class BrokerActiveContracts
    {
        public int Total { get; set; }
        public int Fixed { get; set; }
        public int Variable { get; set; }
        public string BreakdownLabel { get; set; } = string.Empty;
    }

    public class BrokerUpcomingRenewals
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public string PendingLabel { get; set; } = string.Empty;
        public int AvgDaysToExpiry { get; set; }
        public string AvgExpiryLabel { get; set; } = string.Empty;
    }

    public class BrokerMetersManaged
    {
        public int Total { get; set; }
        public int Decreased { get; set; }
        public int Increased { get; set; }
    }

    public class BrokerAnnualUsage
    {
        public decimal TotalMWh { get; set; }
        public string YoYChange { get; set; } = string.Empty;
        public string YoYLabel { get; set; } = string.Empty;
    }

    public class BrokerSummaryMetrics
    {
        public DateTime LastUpdated { get; set; }
        public BrokerCustomersManaged CustomersManaged { get; set; } = new();
        public BrokerActiveContracts ActiveContracts { get; set; } = new();
        public BrokerUpcomingRenewals UpcomingRenewals { get; set; } = new();
        public BrokerMetersManaged MetersManaged { get; set; } = new();
        public BrokerAnnualUsage AnnualUsage { get; set; } = new();
    }

    // ── Recent Customer Activity ───────────────────────────────────────────────

    public class BrokerContractAmendments
    {
        public int Count { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class BrokerOpenSupportRequestsSummary
    {
        public int Count { get; set; }
        public int HighPriority { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class BrokerUpcomingRenewalsSummary
    {
        public int Count { get; set; }
        public int DueWithinDays { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class BrokerEstimatedRevenue
    {
        public decimal Amount { get; set; }
        public int RenewalCount { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class BrokerRecentActivity
    {
        public DateTime LastUpdated { get; set; }
        public BrokerContractAmendments ContractAmendmentsSigned { get; set; } = new();
        public BrokerOpenSupportRequestsSummary OpenSupportRequests { get; set; } = new();
        public BrokerUpcomingRenewalsSummary UpcomingContractRenewals { get; set; } = new();
        public BrokerEstimatedRevenue EstimatedRevenueAtRenewal { get; set; } = new();
    }

    // ── Renewal Watchlist ──────────────────────────────────────────────────────

    public class RenewalWatchlistEntry
    {
        public string Customer { get; set; } = string.Empty;
        public decimal BrokerMargin { get; set; }
        public int TermMonths { get; set; }
        public decimal VolumeMWh { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class RenewalWatchlistFilterOption
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RenewalWatchlistResponse
    {
        public int TotalEntries { get; set; }
        public List<RenewalWatchlistFilterOption> Filters { get; set; } = [];
        public List<RenewalWatchlistEntry> Items { get; set; } = [];
    }

    // ── Open Support Requests ──────────────────────────────────────────────────

    public class BrokerSupportRequestEntry
    {
        public string Customer { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
    }

    public class BrokerOpenSupportRequestsResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerSupportRequestEntry> Items { get; set; } = [];
    }
}
