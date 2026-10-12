using System.ComponentModel.DataAnnotations;

namespace BP.Models
{
    public class SupportCaseSummary
    {
        public int OpenCases { get; set; }
        public int HighPriorityCases { get; set; }
        public int AwaitingResponse { get; set; }
        public int CasesClosedThisMonth { get; set; }
    }

    public class SupportCase
    {
        public string CaseId { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Meter { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ServiceRep { get; set; } = string.Empty;
        public string ClosedDate { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;  
    }

    public class SupportCaseListResponse
    {
        public SupportCaseSummary Summary { get; set; } = new();
        public List<SupportCase> Cases { get; set; } = [];
    }

    public class SupportCaseDetailResponse
    {
        public DateTime LastUpdated { get; set; }
        public SupportCase Case { get; set; } = new();
    }

    public class SupportRequest
    {
        [Required]
        public string ContractId { get; set; } = string.Empty;
        [Required]
        public string MeterId { get; set; } = string.Empty;
        [Required]
        public string RequestType { get; set; } = string.Empty;
        [Required]
        public string Priority { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        [Required]
        public string Comment { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Role { get; set; } = "billing";
        public int CustomerId { get; set; }
    }
}
