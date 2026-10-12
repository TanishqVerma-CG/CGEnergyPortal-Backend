using BP.Models;

namespace BP.IService
{
    public interface IBrokerRenewalsService
    {
        BrokerRenewalsSummary GetRenewalsSummary();
        BrokerCustomerRenewalsListResponse GetCustomerRenewalsList(string sortBy, string sortOrder);

        BrokerCustomerRenewalSummary GetCustomerRenewalSummary(string companyName);
        BrokerCustomerContractsResponse GetCustomerContracts(
            string companyName,
            string serviceAddress,
            string expiringContract,
            string sortBy,
            string sortOrder);
        List<string> GetServiceAddressDropdown(string companyName);
        List<BrokerContractDropdownOption> GetContractDropdown(string companyName);
        BrokerContractQuoteHistory? GetQuoteByContractId(string contractId);
        BrokerContractDetails? GetContractDetails(string contractId);
        QuoteStatusResponse ApproveQuote(string quoteId);
        QuoteStatusResponse RejectQuote(string quoteId);
        PricingIntelligenceResponse? GetPricingIntelligence(string contractId, string snapshot, string priceUnit);
        PricingTrendResponse? GetPricingTrend(string contractId);
        PricingSnapshotDetailsResponse? GetPricingSnapshotDetails(string contractId);
        RenewalQuoteDetail? GetRenewalQuoteDetail(string quoteId);
    }
}
