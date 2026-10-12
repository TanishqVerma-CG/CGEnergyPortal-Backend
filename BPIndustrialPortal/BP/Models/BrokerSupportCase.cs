namespace BP.Models
{
    public class BrokerSupportCaseSummary
    {
        public int OpenCases { get; set; }
        public int HighPriorityCases { get; set; }
        public int AwaitingResponse { get; set; }
        public int CasesClosedThisMonth { get; set; }
    }

    public class BrokerSupportCase
    {
        public string CaseNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string Customer { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string ClosedOn { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastActivity { get; set; } = string.Empty;
        public string RaisedBy { get; set; } = string.Empty;
        public string ContractId { get; set; } = string.Empty;
        public string Meter { get; set; } = string.Empty;
        public string ServiceRep { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    public class BrokerSupportCaseListResponse
    {
        public BrokerSupportCaseSummary Summary { get; set; } = new();
        public List<BrokerSupportCase> Cases { get; set; } = [];
        public int TotalEntries { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalPages { get; set; } = 1;
    }
}
