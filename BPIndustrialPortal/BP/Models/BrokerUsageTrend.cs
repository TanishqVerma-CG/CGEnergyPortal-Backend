namespace BP.Models
{
    public class BrokerUsageTrendDataPoint
    {
        public int Day { get; set; }
        public decimal ActualUsageMWh { get; set; }
        public decimal ContractedBlockedVolumeMWh { get; set; }
        public decimal MarketIndexVolumeMWh { get; set; }
        public decimal AverageTemperatureF { get; set; }
    }

    public class BrokerUsageTrendSummary
    {
        public decimal TotalUsageMWh { get; set; }
        public decimal ContractedBlockedVolumeMWh { get; set; }
        public decimal MarketIndexVolumeMWh { get; set; }
        public decimal ContractCoveragePercent { get; set; }
    }

    public class BrokerUsageTrendMonth
    {
        public string Month { get; set; } = string.Empty;
        public BrokerUsageTrendSummary Summary { get; set; } = new();
        public List<BrokerUsageTrendDataPoint> DataPoints { get; set; } = [];
    }

    public class BrokerUsageTrendResponse
    {
        public string MeterId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<BrokerUsageTrendMonth> Trends { get; set; } = [];
    }
}
