using BP.Models;

namespace BP.IService
{
    public interface IBrokerHomeService
    {
        BrokerSummaryMetrics GetSummaryMetrics();
        BrokerRecentActivity GetRecentActivity();
        RenewalWatchlistResponse GetRenewalWatchlist(string daysFilter, string sortBy, string sortOrder);
        byte[] ExportRenewalWatchlist(string daysFilter);
        BrokerOpenSupportRequestsResponse GetOpenSupportRequests(string sortBy, string sortOrder);
    }
}
