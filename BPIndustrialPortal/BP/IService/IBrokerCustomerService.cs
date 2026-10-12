using BP.Models;

namespace BP.IService
{
    public interface IBrokerCustomerService
    {
        BrokerSummaryCards GetSummaryCards();
        BrokerCustomerPortfolioResponse GetCustomerPortfolio(int? page = null, int? pageSize = null, string? search = null);
        List<BrokerContract> GetCustomerContract(int customerId, string contractId);
        BrokerMeterListResponse GetCustomerMeters(int customerId);
        BrokerUsageTrendResponse GetBrokerUsageTrend(string meterId, DateTime startDate, DateTime endDate);
        List<string> GetCustomerContractIds(int customerId);
        List<string> GetCustomerMeterIds(int customerId);
        List<BrokerCustomerDropdown> GetCustomerListForDropdown();
    }
}
