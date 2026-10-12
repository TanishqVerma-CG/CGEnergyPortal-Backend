using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BP.Models;
using BP.Tests.Helpers;

namespace BP.Tests.Integration;

/// <summary>
/// End-to-end integration tests for BrokerRenewalsController.
/// The real service, real broker-renewals.xml, and real JWT middleware are exercised.
///
/// Real data quick-reference (broker-renewals.xml):
///   5 customers, Acme Manufacturing Ltd. has 4 contracts:
///     CID-10293 – 456 Oak Street, Dallas     – Days=12 – QUOTE SENT
///     CID-10292 – 321 Birch Lane, Austin     – Days=15 – REQUESTED
///     CID-10291 – 321 Birch Lane, Austin     – Days=25 – APPROVED
///     CID-10290 – 123 Maplewood Ave, SA      – Days=35 – REQUESTED
///   Acme summary: Active=4, RenewalPending=2, OpenRequest=1, QuotesApproved=1
///   Acme distinct addresses: 3 (Dallas, Austin, San Antonio)
/// </summary>
public class BrokerRenewalsControllerTests : IClassFixture<BrokerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private const string AcmeName = "Acme Manufacturing Ltd.";
    private const string AcmeDallas = "456 Oak Street, Dallas, TX 75201";
    private const string AcmeAustin = "321 Birch Lane, Austin, TX 78701";

    public BrokerRenewalsControllerTests(BrokerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetBrokerAuth() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBrokerToken());

    private void SetBillingAuth() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBillingToken());

    private void ClearAuth() =>
        _client.DefaultRequestHeaders.Authorization = null;

    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    // ── 401 – no token ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/broker/renewals/GetRenewalsSummary")]
    [InlineData("/api/broker/renewals/GetCustomerRenewalsList")]
    [InlineData($"/api/broker/renewals/GetCustomerRenewalSummary?companyName={AcmeName}")]
    [InlineData($"/api/broker/renewals/GetCustomerContracts?companyName={AcmeName}")]
    [InlineData($"/api/broker/renewals/GetServiceAddressDropdown?companyName={AcmeName}")]
    [InlineData($"/api/broker/renewals/GetContractDropdown?companyName={AcmeName}")]
    [InlineData("/api/broker/renewals/GetQuoteByContractId?contractId=CID-10293")]
    public async Task Endpoint_WithoutToken_Returns401(string path)
    {
        ClearAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 403 – wrong role (Billing token on Broker endpoint) ───────────────────

    [Theory]
    [InlineData("/api/broker/renewals/GetRenewalsSummary")]
    [InlineData("/api/broker/renewals/GetCustomerRenewalsList")]
    public async Task Endpoint_WithBillingToken_Returns403(string path)
    {
        SetBillingAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET GetRenewalsSummary ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRenewalsSummary_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalsSummary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetRenewalsSummary_ResponseDeserializesWithCorrectValues()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalsSummary");
        var result = await DeserializeAsync<BrokerRenewalsSummary>(response);

        result.UpcomingRenewals.Should().Be(35);
        result.CustomersImpacted.Should().Be(12);
        
    }

    // ── GET GetCustomerRenewalsList ────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerRenewalsList_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalsList");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCustomerRenewalsList_ReturnsAllFiveCustomers()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalsList");
        var result = await DeserializeAsync<BrokerCustomerRenewalsListResponse>(response);

        result.TotalEntries.Should().Be(5);
        result.Customers.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetCustomerRenewalsList_AcmeRow_HasCorrectCounts()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalsList");
        var result = await DeserializeAsync<BrokerCustomerRenewalsListResponse>(response);
        var acme = result.Customers.Single(c => c.CompanyName == AcmeName);

        acme.TotalContracts.Should().Be(4);
        acme.ExpiringContracts.Should().Be(3);  // Days 12, 15, 25 are ≤30
        acme.PendingRequests.Should().Be(3);    // QUOTE SENT + REQUESTED + REQUESTED
    }

    [Fact]
    public async Task GetCustomerRenewalsList_SortByCustomerAsc_IsDefaultOrder()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalsList?sortBy=customer&sortOrder=asc");
        var result = await DeserializeAsync<BrokerCustomerRenewalsListResponse>(response);

        result.Customers.Select(c => c.CompanyName)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetCustomerRenewalsList_SortByTotalContractsDesc_Works()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalsList?sortBy=totalContracts&sortOrder=desc");
        var result = await DeserializeAsync<BrokerCustomerRenewalsListResponse>(response);

        result.Customers.Select(c => c.TotalContracts)
            .Should().BeInDescendingOrder();
    }

    // ── GET GetCustomerRenewalSummary – validation ─────────────────────────────

    [Fact]
    public async Task GetCustomerRenewalSummary_MissingCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalSummary");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerRenewalSummary_EmptyCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalSummary?companyName=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerRenewalSummary_WhitespaceCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalSummary?companyName=%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerRenewalSummary_UnknownCompany_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerRenewalSummary?companyName=Unknown+Corp");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomerRenewalSummary_ValidCompany_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetCustomerRenewalSummary?companyName={Uri.EscapeDataString(AcmeName)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCustomerRenewalSummary_AcmeResponse_HasCorrectCardValues()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetCustomerRenewalSummary?companyName={Uri.EscapeDataString(AcmeName)}");
        var result = await DeserializeAsync<BrokerCustomerRenewalSummary>(response);

        result.ActiveContracts.Should().Be(4);
        result.RenewalPending.Should().Be(2);    // CID-10292 + CID-10290 (REQUESTED)
        result.OpenRequest.Should().Be(1);       // CID-10293 (QUOTE SENT)
        result.QuotesApproved.Should().Be(1);    // CID-10291 (APPROVED)
    }

    // ── GET GetCustomerContracts – validation ──────────────────────────────────

    [Fact]
    public async Task GetCustomerContracts_MissingCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerContracts");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerContracts_EmptyCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerContracts?companyName=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomerContracts_UnknownCompany_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetCustomerContracts?companyName=Unknown+Corp");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCustomerContracts_ValidRequest_Returns200WithFourContracts()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetCustomerContracts?companyName={Uri.EscapeDataString(AcmeName)}");
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.TotalEntries.Should().Be(4);
    }

    [Fact]
    public async Task GetCustomerContracts_ServiceAddressFilter_ReturnsFilteredContracts()
    {
        SetBrokerAuth();
        var url = $"/api/broker/renewals/GetCustomerContracts" +
                  $"?companyName={Uri.EscapeDataString(AcmeName)}" +
                  $"&serviceAddress={Uri.EscapeDataString(AcmeAustin)}";
        var response = await _client.GetAsync(url);
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        result.TotalEntries.Should().Be(2); // CID-10292 and CID-10291 both at Austin
        result.Contracts.Should().OnlyContain(c => c.ServiceAddress == AcmeAustin);
    }

    [Fact]
    public async Task GetCustomerContracts_ServiceAddressFilter_UnknownAddress_ReturnsEmpty()
    {
        SetBrokerAuth();
        var url = $"/api/broker/renewals/GetCustomerContracts" +
                  $"?companyName={Uri.EscapeDataString(AcmeName)}" +
                  $"&serviceAddress=999+Unknown+Street";
        var response = await _client.GetAsync(url);
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.TotalEntries.Should().Be(0);
    }

    [Fact]
    public async Task GetCustomerContracts_ExpiringContractFilter_ReturnsSingleContract()
    {
        SetBrokerAuth();
        var url = $"/api/broker/renewals/GetCustomerContracts" +
                  $"?companyName={Uri.EscapeDataString(AcmeName)}" +
                  $"&expiringContract=CID-10293";
        var response = await _client.GetAsync(url);
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        result.TotalEntries.Should().Be(1);
        result.Contracts.Single().ContractId.Should().Be("CID-10293");
    }

    [Fact]
    public async Task GetCustomerContracts_SortByDaysRemainingAsc_FirstItemHasLowestDays()
    {
        SetBrokerAuth();
        var url = $"/api/broker/renewals/GetCustomerContracts" +
                  $"?companyName={Uri.EscapeDataString(AcmeName)}" +
                  $"&sortBy=daysRemaining&sortOrder=asc";
        var response = await _client.GetAsync(url);
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        result.Contracts[0].ContractId.Should().Be("CID-10293"); // Days=12
        result.Contracts.Select(c => c.DaysRemaining).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetCustomerContracts_SortByDaysRemainingDesc_FirstItemHasHighestDays()
    {
        SetBrokerAuth();
        var url = $"/api/broker/renewals/GetCustomerContracts" +
                  $"?companyName={Uri.EscapeDataString(AcmeName)}" +
                  $"&sortBy=daysRemaining&sortOrder=desc";
        var response = await _client.GetAsync(url);
        var result = await DeserializeAsync<BrokerCustomerContractsResponse>(response);

        result.Contracts[0].ContractId.Should().Be("CID-10290"); // Days=35
        result.Contracts.Select(c => c.DaysRemaining).Should().BeInDescendingOrder();
    }

    // ── GET GetServiceAddressDropdown – validation ─────────────────────────────

    [Fact]
    public async Task GetServiceAddressDropdown_MissingCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetServiceAddressDropdown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetServiceAddressDropdown_EmptyCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetServiceAddressDropdown?companyName=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetServiceAddressDropdown_UnknownCompany_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetServiceAddressDropdown?companyName=Unknown+Corp");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetServiceAddressDropdown_AcmeManufacturing_ReturnsThreeDistinctAddresses()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetServiceAddressDropdown?companyName={Uri.EscapeDataString(AcmeName)}");
        var result = await DeserializeAsync<List<string>>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().HaveCount(3); // Dallas, Austin, San Antonio
        result.Should().Contain(AcmeDallas);
        result.Should().Contain(AcmeAustin);
        result.Should().BeInAscendingOrder();
    }

    // ── GET GetContractDropdown – validation ───────────────────────────────────

    [Fact]
    public async Task GetContractDropdown_MissingCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDropdown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContractDropdown_EmptyCompanyName_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDropdown?companyName=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContractDropdown_UnknownCompany_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDropdown?companyName=Unknown+Corp");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContractDropdown_AcmeManufacturing_ReturnsFourContracts()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetContractDropdown?companyName={Uri.EscapeDataString(AcmeName)}");
        var result = await DeserializeAsync<List<BrokerContractDropdownOption>>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().HaveCount(4);
        result.Select(c => c.ContractId).Should().Contain(["CID-10293", "CID-10292", "CID-10291", "CID-10290"]);
    }

    [Fact]
    public async Task GetContractDropdown_EachOption_HasContractIdAndServiceAddress()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetContractDropdown?companyName={Uri.EscapeDataString(AcmeName)}");
        var result = await DeserializeAsync<List<BrokerContractDropdownOption>>(response);

        result.Should().OnlyContain(c =>
            !string.IsNullOrEmpty(c.ContractId) &&
            !string.IsNullOrEmpty(c.ServiceAddress));
    }

    // ── GET GetQuoteByContractId – validation ──────────────────────────────────

    [Fact]
    public async Task GetQuoteByContractId_MissingContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuoteByContractId_EmptyContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId?contractId=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuoteByContractId_UnknownContractId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetQuoteByContractId_ValidContractId_Returns200WithQuotes()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractQuoteHistory>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ContractId.Should().Be("CID-10293");
        result.Quotes.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetQuoteByContractId_AllReturnedQuotes_HaveBrokerMarginSet()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractQuoteHistory>(response);

        result.Quotes.Should().OnlyContain(q => q.BrokerMargin > 0);
    }

    [Fact]
    public async Task GetQuoteByContractId_FirstQuote_HasExpectedFields()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetQuoteByContractId?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractQuoteHistory>(response);
        var first = result.Quotes[0];

        first.QuoteId.Should().NotBeNullOrEmpty();
        first.RequestedPlan.Should().NotBeNullOrEmpty();
        first.TermLength.Should().BeGreaterThan(0);
        first.QuotedPrice.Should().BeGreaterThan(0);
        first.Status.Should().NotBeNullOrEmpty();
    }

    // ── GET GetRenewalQuoteDetail – validation ─────────────────────────────────

    [Fact]
    public async Task GetRenewalQuoteDetail_MissingQuoteId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_EmptyQuoteId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_WhitespaceQuoteId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_UnknownQuoteId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=UNKNOWN-QT");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_ValidQuoteId_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=QT-10033");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_ValidQuoteId_ReturnsCorrectFields()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=QT-10033");
        var result = await DeserializeAsync<RenewalQuoteDetail>(response);

        result.QuoteId.Should().Be("QT-10033");
        result.ContractId.Should().Be("CID-10293");
        result.CompanyName.Should().Be("Acme Manufacturing Ltd.");
        result.ServiceAddress.Should().NotBeNullOrEmpty();
        result.QuotedPrice.Should().BeGreaterThan(0);
        result.BrokerMargin.Should().BeGreaterThan(0);
        result.FinalRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRenewalQuoteDetail_WithoutToken_Returns401()
    {
        ClearAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetRenewalQuoteDetail?quoteId=QT-10033");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET GetContractDetails ────────────────────────────────────────────────

    [Fact]
    public async Task GetContractDetails_WithoutToken_Returns401()
    {
        ClearAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetContractDetails_WithBillingToken_Returns403()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetContractDetails_WithBrokerToken_Returns200Json()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetContractDetails_ResponseContractId_Matches()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.ContractId.Should().Be("CID-10293");
    }

    [Fact]
    public async Task GetContractDetails_ServiceAddress_IsCorrect()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.ServiceAddress.Should().Be(AcmeDallas);
    }

    [Fact]
    public async Task GetContractDetails_ContractTerm_IsPopulated()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.ContractTerm.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContractDetails_StartAndEndDate_ArePopulated()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.StartDate.Should().NotBeNullOrEmpty();
        result.EndDate.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContractDetails_DaysRemaining_IsPositive()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.DaysRemaining.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetContractDetails_ProductTypeAndMeterCount_ArePopulated()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.ProductType.Should().NotBeNullOrEmpty();
        result.MeterCount.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetContractDetails_ContractStatus_IsActive()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10293");
        var result = await DeserializeAsync<BrokerContractDetails>(response);

        result.ContractStatus.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetContractDetails_MissingContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContractDetails_EmptyContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContractDetails_WhitespaceContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=%20%20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContractDetails_UnknownContractId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContractDetails_FindsContractFromNonAcmeCustomer()
    {
        // CID-10285 belongs to a different customer — verifies cross-customer lookup
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetContractDetails?contractId=CID-10285");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
