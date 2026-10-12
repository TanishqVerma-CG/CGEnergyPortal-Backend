namespace BP.Models
{
    public class BrokerSummaryCards
    {
        public int TotalCustomers { get; set; }
        public int ActiveContracts { get; set; }
        public int MeterCount { get; set; }
        public double TotalAnnualLoad { get; set; }
        public string TotalAnnualLoadUnit { get; set; } = string.Empty;
        public int ExpiringIn90Days { get; set; }
    }

    public class BrokerCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ActiveContracts { get; set; }
        public int ActiveMeters { get; set; }
        public double AnnualUsage { get; set; }
        public string AnnualUsageUnit { get; set; } = string.Empty;
        public int ExpiringContracts { get; set; }
        public int ExpiredContracts { get; set; }
    }

    // New DTO used for dropdowns / select lists
    public class BrokerCustomerDropdown
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class BrokerCustomerPortfolioResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerCustomer> Customers { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalPages { get; set; } = 1;
    }

    public class BrokerCustomerPagination
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
    }

    public class BrokerCustomerDashboardResponse
    {
        public BrokerSummaryCards SummaryCards { get; set; } = new();
        public BrokerCustomerPortfolioResponse CustomerPortfolio { get; set; } = new();
        public BrokerCustomerPagination Pagination { get; set; } = new();
    }

    public class BrokerContract
    {
        public string ContractId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int MeterCount { get; set; }
        public double AnnualUsage { get; set; }
        public string AnnualUsageUnit { get; set; } = string.Empty;
        public int ContractTermMonths { get; set; }
        public double BrokerMargin { get; set; }
        public double Price { get; set; }
        public string Product { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
    }

    public class BrokerMeter
    {
        public string MeterId { get; set; } = string.Empty;
        public string UAN { get; set; } = string.Empty;
        public string LDC { get; set; } = string.Empty;
        public string Utility { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string StopDate { get; set; } = string.Empty;
        public double AnnualUsage { get; set; }
        public string AnnualUsageUnit { get; set; } = string.Empty;
    }

    public class BrokerMeterListResponse
    {
        public int TotalEntries { get; set; }
        public List<BrokerMeter> Meters { get; set; } = [];
    }

    public class BrokerCustomerAccountResponse
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<BrokerContract> Contracts { get; set; } = [];
        public BrokerMeterListResponse Meters { get; set; } = new();
    }
}
