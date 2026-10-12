namespace BP.Models
{
    public class MeterDropdownItem
    {
        public string MeterId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class MeterDropdownResponse
    {
        public DateTime LastUpdated { get; set; }
        public List<MeterDropdownItem> Meters { get; set; } = [];
    }

    public class CurrentMonthUsage
    {
        public decimal UsageMWh { get; set; }
        public decimal VsLastMonthPercent { get; set; }
        public string VsLastMonthDirection { get; set; } = string.Empty;  
    }

    public class UsageVsContracted
    {
        public decimal UtilizedPercent { get; set; }
        public decimal UsedMWh { get; set; }
        public decimal ContractedMWh { get; set; }
    }

    public class PeakDemand
    {
        public decimal PeakKW { get; set; }
        public string PeakDate { get; set; } = string.Empty;
    }

    public class YoYChange
    {
        public decimal ChangePercent { get; set; }
        public string Direction { get; set; } = string.Empty;  
        public decimal ComparedMWh { get; set; }
        public int ComparedYear { get; set; }
    }

    public class MeterSummary
    {
        public string MeterId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public CurrentMonthUsage CurrentMonthUsage { get; set; } = new();
        public UsageVsContracted UsageVsContracted { get; set; } = new();
        public PeakDemand PeakDemand { get; set; } = new();
        public YoYChange YoYChange { get; set; } = new();
    }

    // ?? Contract-based usage models ??????????????????????????????????????????

    public class ContractUsageMeterItem
    {
        public string MeterId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ContractUsageMeterDropdownResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public List<ContractUsageMeterItem> Meters { get; set; } = [];
    }

    public class TotalUsageSummary
    {
        public decimal TotalMWh { get; set; }
        public decimal VsLastMonthPercent { get; set; }
        public string VsLastMonthDirection { get; set; } = string.Empty;
    }

    public class ContractCurrentMonthUsage
    {
        public decimal UsageMWh { get; set; }
        public decimal ContractedMWh { get; set; }
    }

    public class UsageVariance
    {
        public decimal ChangePercent { get; set; }
        public string Direction { get; set; } = string.Empty;
        public decimal ComparedMWh { get; set; }
        public int ComparedYear { get; set; }
    }

    public class ContractMeterSummary
    {
        public string ContractId { get; set; } = string.Empty;
        public string MeterId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public TotalUsageSummary TotalUsage { get; set; } = new();
        public ContractCurrentMonthUsage CurrentMonthUsage { get; set; } = new();
        public PeakDemand PeakDemand { get; set; } = new();
        public UsageVariance UsageVariance { get; set; } = new();
    }
    public class MeterLabel
    {
        public string MeterId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class MeterTableRow
    {
        public string MeterId { get; set; } = string.Empty;
        public string UAN { get; set; } = string.Empty;
        public string LDC { get; set; } = string.Empty;
        public string Utility { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string StopDate { get; set; } = string.Empty;
        public long AnnualUsage { get; set; }
    }

    public class MeterTableResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public List<MeterTableRow> Meters { get; set; } = [];
    }
}
