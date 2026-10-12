using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class PricingCell
    {
        public decimal Price { get; set; }
        public string PriceRange { get; set; } = string.Empty;
    }

    public class PricingTermColumn
    {
        public int TermMonths { get; set; }
        public bool IsRecommended { get; set; }
    }

    public class PricingMatrixRow
    {
        public string Label { get; set; } = string.Empty;
        public List<PricingCell> Prices { get; set; } = [];
    }

    public class PricingIntelligenceResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public string PriceUnit { get; set; } = string.Empty;
        public string Snapshot { get; set; } = string.Empty;
        public string SnapshotLabel { get; set; } = string.Empty;
        public List<PricingTermColumn> TermColumns { get; set; } = [];
        public List<PricingMatrixRow> Rows { get; set; } = [];
    }

    public class PricingTrendDataPoint
    {
        public string Date { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsToday { get; set; }
    }

    public class PricingTrendSeries
    {
        public int TermMonths { get; set; }
        public List<PricingTrendDataPoint> DataPoints { get; set; } = [];
    }

    public class PricingTrendResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public string LoadType { get; set; } = string.Empty;
        public string PriceUnit { get; set; } = string.Empty;
        public List<PricingTrendSeries> Series { get; set; } = [];
    }

    public class TodaysLowestPrice
    {
        public int TermMonths { get; set; }
        public decimal Price { get; set; }
        public string PriceUnit { get; set; } = string.Empty;
        public decimal ChangeVsYesterday { get; set; }
        public decimal ChangePercentVsYesterday { get; set; }
    }

    public class PricingSnapshotDetailsResponse
    {
        public string ContractId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string PricingZone { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public TodaysLowestPrice TodaysLowestPrice { get; set; } = new();
    }

    public class FormalQuoteRequest
    {
        [Required]
        public string ContractId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Term length must be a positive number.")]
        public int TermLength { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Estimated Annual usage must be a positive number.")]
        public long EstimatedAnnualUsage { get; set; }
    }

    public class FormalQuoteResponse
    {
        public string QuoteId { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public int TermLength { get; set; }
        public string Description { get; set; } = string.Empty;
        public long EstimatedAnnualUsage { get; set; }
        public string SubmittedAt { get; set; } = string.Empty;
    }
}
