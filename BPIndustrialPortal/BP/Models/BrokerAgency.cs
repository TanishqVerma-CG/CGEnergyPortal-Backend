namespace BP.Models
{
    // ── Broker / Agency Search ─────────────────────────────────────────────────

    public class BrokerSearchResult
    {
        public string AgencyId { get; set; } = string.Empty;
        public string AgencyName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public int NumberOfContracts { get; set; }
    }

    public class BrokerSearchResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerSearchResult> Results { get; set; } = [];
    }

    // ── Agency Dashboard ───────────────────────────────────────────────────────

    public class AgencyAddress
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
    }

    public class AgencyDetail
    {
        public string AgencyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AgencyAddress Address { get; set; } = new();
    }

    public class OnFlowDealPoint
    {
        public string Month { get; set; } = string.Empty;
        public int DealCount { get; set; }
    }

    public class PaymentChartPoint
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsForecasted { get; set; }
    }

    public class AgencySummaryMetrics
    {
        public int NumberOfAgents { get; set; }
        public int NumberOfDeals { get; set; }
        public decimal CurrentPayment { get; set; }
        public string CurrentPaymentPeriod { get; set; } = string.Empty;
        public decimal PreviousPayment { get; set; }
        public string PreviousPaymentPeriod { get; set; } = string.Empty;
        public decimal ForecastedPayment { get; set; }
        public decimal YearToDatePayment { get; set; }
    }

    public class AgentItem
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int ActiveDeals { get; set; }
    }

    public class AgencyDashboardResponse
    {
        public AgencyDetail Agency { get; set; } = new();
        public List<OnFlowDealPoint> OnFlowDeals { get; set; } = [];
        public List<PaymentChartPoint> PaymentChart { get; set; } = [];
        public AgencySummaryMetrics AgencySummary { get; set; } = new();
        public List<AgentItem> Agents { get; set; } = [];
    }
}
