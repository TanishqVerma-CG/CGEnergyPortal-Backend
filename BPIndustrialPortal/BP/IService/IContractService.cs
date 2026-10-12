using BP.Models;

namespace BP.IService
{
    public interface IContractService
    {
        ContractDropdownResponse GetContractDropdown();
        ContractSummary? GetContractSummary(string contractId);
        List<ContractHistoryEntry>? GetContractHistory(string contractId);
        ContractDetail? GetContractDetailById(string id);
        MeterTableResponse? GetMetersByContractId(string contractId);
        ContractTableResponse GetContracts();
        RenewContractsResponse GetRenewContracts();
        PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot, string priceUnit);
        PricingTrendResponse? GetPricingTrend(string contractId);
        PricingSnapshotDetailsResponse? GetPricingSnapshotDetails(string contractId);
        FormalQuoteResponse SubmitFormalQuote(FormalQuoteRequest request);
        RenewalTableResponse GetRenewalContracts();
        ContractQuoteHistory? GetQuoteByContractId(string contractId);
        List<string> GetRenewalAddressDropdown();
        RenewalSummary GetRenewalSummary();
        RenewalQuoteDetail? GetRenewalQuoteDetail(string quoteId);
        RenewContractResponse RequestContractRenewal(RenewContractRequest request);
        ContractInfoResponse? GetContractInfo(string contractId);
    }
}
