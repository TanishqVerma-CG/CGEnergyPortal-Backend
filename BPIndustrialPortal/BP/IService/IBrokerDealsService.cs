using BP.Models;

namespace BP.IService
{
    public interface IBrokerDealsService
    {
        DealsResponse GetMyDeals(string? agencyId = null);
        byte[] ExportDeals(string? agencyId = null);
    }
}
