namespace BP.Models
{
    public class UsageTrendRequest
    {
        public string ContractId { get; set; } = string.Empty;
        public string MeterId { get; set; } = "all";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class UsageTrendDataPoint
    {
        public int Day { get; set; }
        public decimal ActualUsageMWh { get; set; }
        public decimal ContractedBlockedVolumeMWh { get; set; }
        public decimal MarketIndexVolumeMWh { get; set; }
        public decimal AverageTemperatureF { get; set; }
    }

    public class UsageTrendSummary
    {
        public decimal TotalUsageMWh { get; set; }
        public decimal ContractedBlockedVolumeMWh { get; set; }
        public decimal MarketIndexVolumeMWh { get; set; }
        public decimal ContractCoveragePercent { get; set; }
    }

    public class UsageTrendResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public string MeterId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<UsageTrendDataPoint> DataPoints { get; set; } = [];
        public UsageTrendSummary Summary { get; set; } = new();
    }
}
