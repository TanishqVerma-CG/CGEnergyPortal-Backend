using BP.Models;

namespace BP.IService
{
    public interface IBrokerAgencyService
    {
        BrokerSearchResponse SearchBrokers(string? contactName, string? agencyName);
        AgencyDashboardResponse? GetAgencyDashboard(string agencyId);
    }
}
