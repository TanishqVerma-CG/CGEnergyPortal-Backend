using BP.Services;
using BP.Tests.Helpers;
using BP.IService;

namespace BP.Tests.Unit;

/// <summary>
/// Unit tests for BrokerRenewalsService using isolated temp XML (see TestXmlData.BrokerRenewals).
///
/// Test data quick-reference:
///   Alpha Customer (3 contracts):
///     TEST-C001 – 100 Main Street – Days=10 – QUOTE SENT  → expiring, open
///     TEST-C002 – 100 Main Street – Days=20 – REQUESTED   → expiring, pending
///     TEST-C003 – 200 Oak Avenue  – Days=45 – APPROVED    → not expiring
///     TotalContracts=3, ExpiringContracts(≤30)=2, PendingRequests=2
///     Cards: Active=3, RenewalPending=1, OpenRequest=1, QuotesApproved=1
///
///   Beta Customer (2 contracts):
///     TEST-C004 – 300 Pine Road   – Days=5  – REQUESTED   → expiring, pending
///     TEST-C005 – 400 Elm Street  – Days=60 – APPROVED    → not expiring
///     TotalContracts=2, ExpiringContracts(≤30)=1, PendingRequests=1
///     Cards: Active=2, RenewalPending=1, OpenRequest=0, QuotesApproved=1
/// </summary>
public class BrokerRenewalsServiceTests : IClassFixture<BrokerRenewalsServiceFixture>
{
    private readonly BrokerRenewalsService _sut;

    public BrokerRenewalsServiceTests(BrokerRenewalsServiceFixture fixture)
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        var contractService = Mock.Of<IContractService>();
        _sut = new BrokerRenewalsService(env, contractService);
    }

    // ── GetRenewalsSummary ─────────────────────────────────────────────────────

    [Fact]
    public void GetRenewalsSummary_ReturnsCorrectUpcomingRenewals()
    {
        _sut.GetRenewalsSummary().UpcomingRenewals.Should().Be(20);
    }

    [Fact]
    public void GetRenewalsSummary_ReturnsCorrectCustomersImpacted()
    {
        _sut.GetRenewalsSummary().CustomersImpacted.Should().Be(5);
    }

  

    [Fact]
    public void GetRenewalsSummary_MissingXmlFile_ThrowsFileNotFoundException()
    {
        var badEnv = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == @"C:\NonExistent\DoesNotExist");
        var contractService = Mock.Of<IContractService>();
        var sut = new BrokerRenewalsService(badEnv, contractService);

        var act = () => sut.GetRenewalsSummary();

        act.Should().Throw<FileNotFoundException>();
    }

    // ── GetCustomerRenewalsList ────────────────────────────────────────────────

    [Fact]
    public void GetCustomerRenewalsList_TotalEntries_IsTwo()
    {
        var result = _sut.GetCustomerRenewalsList("customer", "asc");

        result.TotalEntries.Should().Be(2);
        result.Customers.Should().HaveCount(2);
    }

    [Fact]
    public void GetCustomerRenewalsList_AlphaCustomer_HasCorrectCounts()
    {
        var result = _sut.GetCustomerRenewalsList("customer", "asc");
        var alpha = result.Customers.Single(c => c.CompanyName == "Alpha Customer");

        alpha.TotalContracts.Should().Be(3);
        alpha.ExpiringContracts.Should().Be(2);  // Days 10 and 20 are ≤30
        alpha.PendingRequests.Should().Be(2);    // QUOTE SENT + REQUESTED
    }

    [Fact]
    public void GetCustomerRenewalsList_BetaCustomer_HasCorrectCounts()
    {
        var result = _sut.GetCustomerRenewalsList("customer", "asc");
        var beta = result.Customers.Single(c => c.CompanyName == "Beta Customer");

        beta.TotalContracts.Should().Be(2);
        beta.ExpiringContracts.Should().Be(1);  // Only Days=5 is ≤30
        beta.PendingRequests.Should().Be(1);    // Only REQUESTED
    }

    [Fact]
    public void GetCustomerRenewalsList_DefaultSort_CustomerNameAscending()
    {
        var result = _sut.GetCustomerRenewalsList("customer", "asc");

        result.Customers[0].CompanyName.Should().Be("Alpha Customer");
        result.Customers[1].CompanyName.Should().Be("Beta Customer");
    }

    [Fact]
    public void GetCustomerRenewalsList_SortByCustomerDesc_ReversesOrder()
    {
        var result = _sut.GetCustomerRenewalsList("customer", "desc");

        result.Customers[0].CompanyName.Should().Be("Beta Customer");
        result.Customers[1].CompanyName.Should().Be("Alpha Customer");
    }

    [Fact]
    public void GetCustomerRenewalsList_SortByTotalContractsAsc_BetaFirst()
    {
        var result = _sut.GetCustomerRenewalsList("totalContracts", "asc");

        result.Customers[0].CompanyName.Should().Be("Beta Customer");  // 2
        result.Customers[1].CompanyName.Should().Be("Alpha Customer"); // 3
    }

    [Fact]
    public void GetCustomerRenewalsList_SortByTotalContractsDesc_AlphaFirst()
    {
        var result = _sut.GetCustomerRenewalsList("totalContracts", "desc");

        result.Customers[0].CompanyName.Should().Be("Alpha Customer"); // 3
        result.Customers[1].CompanyName.Should().Be("Beta Customer");  // 2
    }

    [Fact]
    public void GetCustomerRenewalsList_SortByExpiringContractsAsc_BetaFirst()
    {
        var result = _sut.GetCustomerRenewalsList("expiringContracts", "asc");

        result.Customers[0].ExpiringContracts.Should().Be(1); // Beta
        result.Customers[1].ExpiringContracts.Should().Be(2); // Alpha
    }

    [Fact]
    public void GetCustomerRenewalsList_SortByPendingRequestsAsc_BetaFirst()
    {
        var result = _sut.GetCustomerRenewalsList("pendingRequests", "asc");

        result.Customers[0].PendingRequests.Should().Be(1); // Beta
        result.Customers[1].PendingRequests.Should().Be(2); // Alpha
    }

    // ── GetCustomerRenewalSummary ──────────────────────────────────────────────

    [Fact]
    public void GetCustomerRenewalSummary_AlphaCustomer_ReturnsCorrectCards()
    {
        var result = _sut.GetCustomerRenewalSummary("Alpha Customer");

        result.ActiveContracts.Should().Be(3);
        result.RenewalPending.Should().Be(1);   // Only REQUESTED: TEST-C002
        result.OpenRequest.Should().Be(1);      // Only QUOTE SENT: TEST-C001
        result.QuotesApproved.Should().Be(1);   // Only APPROVED: TEST-C003
    }

    [Fact]
    public void GetCustomerRenewalSummary_BetaCustomer_ReturnsCorrectCards()
    {
        var result = _sut.GetCustomerRenewalSummary("Beta Customer");

        result.ActiveContracts.Should().Be(2);
        result.RenewalPending.Should().Be(1);   // TEST-C004 REQUESTED
        result.OpenRequest.Should().Be(0);      // No QUOTE SENT
        result.QuotesApproved.Should().Be(1);   // TEST-C005 APPROVED
    }

    [Fact]
    public void GetCustomerRenewalSummary_CompanyNameIsCaseInsensitive()
    {
        var result = _sut.GetCustomerRenewalSummary("ALPHA CUSTOMER");

        result.ActiveContracts.Should().Be(3);
    }

    [Fact]
    public void GetCustomerRenewalSummary_UnknownCustomer_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetCustomerRenewalSummary("No Such Company");

        act.Should().Throw<KeyNotFoundException>()
           .WithMessage("*No Such Company*");
    }

    // ── GetCustomerContracts ───────────────────────────────────────────────────

    [Fact]
    public void GetCustomerContracts_NoFilters_ReturnsAllThreeContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(3);
        result.Contracts.Should().HaveCount(3);
    }

    [Fact]
    public void GetCustomerContracts_TotalEntries_MatchesContractListCount()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(result.Contracts.Count);
    }

    [Fact]
    public void GetCustomerContracts_FilterByServiceAddress_ReturnsTwoContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "100 Main Street", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(2);
        result.Contracts.Should().OnlyContain(c => c.ServiceAddress == "100 Main Street");
    }

    [Fact]
    public void GetCustomerContracts_FilterByServiceAddressAll_ReturnsAllContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetCustomerContracts_FilterByServiceAddressEmpty_ReturnsAllContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetCustomerContracts_FilterByServiceAddressWhitespace_ReturnsAllContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "   ", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetCustomerContracts_FilterByServiceAddress_AddressNotFound_ReturnsEmpty()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "999 Unknown Blvd", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(0);
        result.Contracts.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomerContracts_FilterByExpiringContract_ReturnsSingleContract()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "TEST-C002", "contractId", "asc");

        result.TotalEntries.Should().Be(1);
        result.Contracts.Single().ContractId.Should().Be("TEST-C002");
    }

    [Fact]
    public void GetCustomerContracts_FilterByExpiringContractAll_ReturnsAllContracts()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "asc");

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetCustomerContracts_BothFilters_NoOverlap_ReturnsEmpty()
    {
        // TEST-C001 is at "100 Main Street", not "200 Oak Avenue"
        var result = _sut.GetCustomerContracts("Alpha Customer", "200 Oak Avenue", "TEST-C001", "contractId", "asc");

        result.TotalEntries.Should().Be(0);
        result.Contracts.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomerContracts_BothFilters_WithOverlap_ReturnsMatchingContract()
    {
        // TEST-C001 IS at "100 Main Street"
        var result = _sut.GetCustomerContracts("Alpha Customer", "100 Main Street", "TEST-C001", "contractId", "asc");

        result.TotalEntries.Should().Be(1);
        result.Contracts.Single().ContractId.Should().Be("TEST-C001");
    }

    [Fact]
    public void GetCustomerContracts_SortByDaysRemainingAsc_FirstItemHasLowestDays()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "daysRemaining", "asc");

        result.Contracts[0].ContractId.Should().Be("TEST-C001"); // Days=10
        result.Contracts[1].ContractId.Should().Be("TEST-C002"); // Days=20
        result.Contracts[2].ContractId.Should().Be("TEST-C003"); // Days=45
    }

    [Fact]
    public void GetCustomerContracts_SortByDaysRemainingDesc_FirstItemHasHighestDays()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "daysRemaining", "desc");

        result.Contracts[0].ContractId.Should().Be("TEST-C003"); // Days=45
        result.Contracts[1].ContractId.Should().Be("TEST-C002"); // Days=20
        result.Contracts[2].ContractId.Should().Be("TEST-C001"); // Days=10
    }

    [Fact]
    public void GetCustomerContracts_SortByContractIdAsc_OrderedAlphabetically()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "asc");

        result.Contracts.Select(c => c.ContractId)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetCustomerContracts_SortByContractIdDesc_OrderedReverseAlphabetically()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "contractId", "desc");

        result.Contracts.Select(c => c.ContractId)
            .Should().BeInDescendingOrder();
    }

    [Fact]
    public void GetCustomerContracts_SortByLastQuoteStatusAsc_ApprovedFirst()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "all", "lastQuoteStatus", "asc");

        result.Contracts[0].LastQuoteStatus.Should().Be("APPROVED");  // TEST-C003
    }

    [Fact]
    public void GetCustomerContracts_ContractRowFields_AreMappedCorrectly()
    {
        var result = _sut.GetCustomerContracts("Alpha Customer", "all", "TEST-C001", "contractId", "asc");
        var contract = result.Contracts.Single();

        contract.ContractId.Should().Be("TEST-C001");
        contract.ServiceAddress.Should().Be("100 Main Street");
        contract.ContractStatus.Should().Be("ACTIVE");
        contract.CurrentPlan.Should().Be(24);
        contract.EndDate.Should().Be("7/20/2026");
        contract.DaysRemaining.Should().Be(10);
        contract.LastQuoteStatus.Should().Be("QUOTE SENT");
    }

    [Fact]
    public void GetCustomerContracts_UnknownCustomer_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetCustomerContracts("No Such Company", "all", "all", "contractId", "asc");

        act.Should().Throw<KeyNotFoundException>();
    }

    // ── GetServiceAddressDropdown ──────────────────────────────────────────────

    [Fact]
    public void GetServiceAddressDropdown_Alpha_ReturnsTwoDistinctAddresses()
    {
        var result = _sut.GetServiceAddressDropdown("Alpha Customer");

        result.Should().HaveCount(2);
        result.Should().Contain("100 Main Street");
        result.Should().Contain("200 Oak Avenue");
    }

    [Fact]
    public void GetServiceAddressDropdown_Alpha_AddressesAreSortedAlphabetically()
    {
        var result = _sut.GetServiceAddressDropdown("Alpha Customer");

        result.Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetServiceAddressDropdown_Beta_ReturnsTwoDistinctAddresses()
    {
        var result = _sut.GetServiceAddressDropdown("Beta Customer");

        result.Should().HaveCount(2);
        result.Should().Contain("300 Pine Road");
        result.Should().Contain("400 Elm Street");
    }

    [Fact]
    public void GetServiceAddressDropdown_UnknownCustomer_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetServiceAddressDropdown("No Such Company");

        act.Should().Throw<KeyNotFoundException>();
    }

    // ── GetContractDropdown ────────────────────────────────────────────────────

    [Fact]
    public void GetContractDropdown_Alpha_ReturnsThreeContracts()
    {
        var result = _sut.GetContractDropdown("Alpha Customer");

        result.Should().HaveCount(3);
    }

    [Fact]
    public void GetContractDropdown_Alpha_AllContractIdsPresent()
    {
        var result = _sut.GetContractDropdown("Alpha Customer");

        result.Select(c => c.ContractId).Should().Contain(["TEST-C001", "TEST-C002", "TEST-C003"]);
    }

    [Fact]
    public void GetContractDropdown_Alpha_ServiceAddressIsMappedPerContract()
    {
        var result = _sut.GetContractDropdown("Alpha Customer");
        var c001 = result.Single(c => c.ContractId == "TEST-C001");

        c001.ServiceAddress.Should().Be("100 Main Street");
    }

    [Fact]
    public void GetContractDropdown_UnknownCustomer_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetContractDropdown("No Such Company");

        act.Should().Throw<KeyNotFoundException>();
    }

    // ── GetQuoteByContractId ───────────────────────────────────────────────────

    [Fact]
    public void GetQuoteByContractId_KnownContract_ReturnsTwoQuotes()
    {
        var result = _sut.GetQuoteByContractId("TEST-C001");

        result.Should().NotBeNull();
        result!.ContractId.Should().Be("TEST-C001");
        result.Quotes.Should().HaveCount(2);
    }

    [Fact]
    public void GetQuoteByContractId_QuoteFields_AreMappedCorrectly()
    {
        var result = _sut.GetQuoteByContractId("TEST-C001");
        var firstQuote = result!.Quotes[0];

        firstQuote.QuoteId.Should().Be("QT-TA001");
        firstQuote.RequestedOn.Should().Be("5/15/2026");
        firstQuote.RequestedPlan.Should().Be("Market Index");
        firstQuote.TermLength.Should().Be(24);
        firstQuote.QuotedPrice.Should().Be(62.00m);
        firstQuote.BrokerMargin.Should().Be(0.025m);
        firstQuote.Status.Should().Be("QUOTE SENT");
        firstQuote.SentOn.Should().Be("5/25/2026");
    }

    [Fact]
    public void GetQuoteByContractId_EachQuoteHasBrokerMarginSet()
    {
        var result = _sut.GetQuoteByContractId("TEST-C001");

        result!.Quotes.Should().OnlyContain(q => q.BrokerMargin > 0);
    }

    [Fact]
    public void GetQuoteByContractId_ContractInSecondCustomer_IsFound()
    {
        // TEST-C004 belongs to Beta Customer, not Alpha
        var result = _sut.GetQuoteByContractId("TEST-C004");

        result.Should().NotBeNull();
        result!.ContractId.Should().Be("TEST-C004");
        result.Quotes.Single().BrokerMargin.Should().Be(0.050m);
    }

    [Fact]
    public void GetQuoteByContractId_SentOnEmpty_IsReturnedAsEmptyString()
    {
        // QT-TA003 in TEST-C002 has empty SentOn
        var result = _sut.GetQuoteByContractId("TEST-C002");
        var requestedQuote = result!.Quotes.Single(q => q.Status == "REQUESTED");

        requestedQuote.SentOn.Should().BeEmpty();
    }

    [Fact]
    public void GetQuoteByContractId_UnknownContractId_ReturnsNull()
    {
        var result = _sut.GetQuoteByContractId("UNKNOWN-CONTRACT");

        result.Should().BeNull();
    }

    // ── GetContractDetails ────────────────────────────────────────────────────

    [Fact]
    public void GetContractDetails_KnownContractId_ReturnsResult()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetContractDetails_ContractId_RoundTrips()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.ContractId.Should().Be("TEST-C001");
    }

    [Fact]
    public void GetContractDetails_ServiceAddress_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.ServiceAddress.Should().Be("100 Main Street");
    }

    [Fact]
    public void GetContractDetails_ContractTerm_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.ContractTerm.Should().Be("24");
    }

    [Fact]
    public void GetContractDetails_StartDate_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.StartDate.Should().Be("1/1/2024");
    }

    [Fact]
    public void GetContractDetails_EndDate_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.EndDate.Should().Be("7/20/2026");
    }

    [Fact]
    public void GetContractDetails_DaysRemaining_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.DaysRemaining.Should().Be(10);
    }

    [Fact]
    public void GetContractDetails_ProductType_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.ProductType.Should().Be("Fixed");
    }

    [Fact]
    public void GetContractDetails_MeterCount_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.MeterCount.Should().Be("3");
    }

    [Fact]
    public void GetContractDetails_ContractStatus_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.ContractStatus.Should().Be("ACTIVE");
    }

    [Fact]
    public void GetContractDetails_LastQuoteStatus_IsCorrect()
    {
        var result = _sut.GetContractDetails("TEST-C001");

        result!.LastQuoteStatus.Should().Be("QUOTE SENT");
    }

    [Fact]
    public void GetContractDetails_FindsContractAcrossAllCustomers()
    {
        // TEST-C004 belongs to Beta Customer — verifies cross-customer search
        var result = _sut.GetContractDetails("TEST-C004");

        result.Should().NotBeNull();
        result!.ContractId.Should().Be("TEST-C004");
    }

    [Fact]
    public void GetContractDetails_ContractIdIsCaseInsensitive()
    {
        var result = _sut.GetContractDetails("test-c001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetContractDetails_UnknownContractId_ReturnsNull()
    {
        var result = _sut.GetContractDetails("UNKNOWN-CONTRACT");

        result.Should().BeNull();
    }

    [Fact]
    public void GetContractDetails_MissingXmlFile_ThrowsFileNotFoundException()
    {
        var badEnv = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == @"C:\NonExistent\DoesNotExist");
        var contractService = Mock.Of<IContractService>();
        var _sut = new BrokerRenewalsService(badEnv, contractService);

        var act = () => _sut.GetContractDetails("TEST-C001");

        act.Should().Throw<FileNotFoundException>();
    }
}
