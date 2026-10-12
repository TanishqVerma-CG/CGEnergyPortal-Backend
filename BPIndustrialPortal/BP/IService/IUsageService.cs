using BP.Models;

namespace BP.IService
{
    public interface IUsageService
    {
        MeterDropdownResponse GetMeterDropdown();
        MeterSummary? GetMeterSummary(string meterId);
        ContractUsageMeterDropdownResponse GetContractMeterDropdown(string contractId);
        ContractMeterSummary? GetContractMeterSummary(string contractId, string meterId);
        List<MeterLabel> GetMeterIdsByContractId(string contractId);
        UsageTrendResponse? GetUsageTrend(string contractId, string meterId, DateTime startDate, DateTime endDate);
    }
}
