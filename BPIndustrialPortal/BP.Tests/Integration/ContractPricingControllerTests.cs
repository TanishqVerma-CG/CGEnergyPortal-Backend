using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BP.Models;
using BP.Tests.Helpers;

namespace BP.Tests.Integration;

/// <summary>
/// Integration tests for all six pricing endpoints across two controllers:
///
///   ContractController  (Billing policy)
///     GET api/GetPricingIntelligence?contractId=&amp;snapshot=&amp;priceUnit=
///     GET api/GetPricingTrend?contractId=
///     GET api/GetPricingSnapshotDetails?contractId=
///
///   BrokerRenewalsController  (Broker policy)
///     GET api/broker/renewals/GetPricingIntelligence?contractId=&amp;snapshot=&amp;priceUnit=
///     GET api/broker/renewals/GetPricingTrend?contractId=
///     GET api/broker/renewals/GetPricingSnapshotDetails?contractId=
///
/// Real data used (from pricing-intelligence.xml / pricing-trend.xml / pricing-snapshot-details.xml):
///   contractId = 2453, snapshot = today
///   PriceUnit  = $/MWh
///   BestValue  = Off-Peak 36M ($45.10) and All Hours 36M ($51.20)
///   Trend      = 4 series (12M, 24M, 36M, 48M), 9 data points each
///   Snapshot   = ERCOT North, Standard Fixed, lowest $45.10 (-1.20 / -2.6%)
/// </summary>
public class ContractPricingControllerTests : IClassFixture<BrokerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private const string ValidContractId = "2453";
    private const string ValidSnapshot   = "today";

    public ContractPricingControllerTests(BrokerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetBillingAuth() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBillingToken());

    private void SetBrokerAuth() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBrokerToken());

    private void ClearAuth() =>
        _client.DefaultRequestHeaders.Authorization = null;

    private async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RBAC — 401 (no token) on all six endpoints
    // ══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}")]
    [InlineData($"/api/GetPricingTrend?contractId={ValidContractId}")]
    [InlineData($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}")]
    [InlineData($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}")]
    [InlineData($"/api/broker/renewals/GetPricingTrend?contractId={ValidContractId}")]
    [InlineData($"/api/broker/renewals/GetPricingSnapshotDetails?contractId={ValidContractId}")]
    public async Task AllPricingEndpoints_WithoutToken_Return401(string path)
    {
        ClearAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RBAC — 403 (wrong role) cross-policy tests
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Broker token must be rejected by Billing-policy endpoints.</summary>
    [Theory]
    [InlineData($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}")]
    [InlineData($"/api/GetPricingTrend?contractId={ValidContractId}")]
    [InlineData($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}")]
    public async Task BillingEndpoints_WithBrokerToken_Return403(string path)
    {
        SetBrokerAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>Billing token must be rejected by Broker-policy endpoints.</summary>
    [Theory]
    [InlineData($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}")]
    [InlineData($"/api/broker/renewals/GetPricingTrend?contractId={ValidContractId}")]
    [InlineData($"/api/broker/renewals/GetPricingSnapshotDetails?contractId={ValidContractId}")]
    public async Task BrokerPricingEndpoints_WithBillingToken_Return403(string path)
    {
        SetBillingAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/GetPricingIntelligence  (Billing)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingIntelligence_Billing_ValidParams_Returns200Json()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_ResponseContractIdAndPriceUnit()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.ContractId.Should().Be(ValidContractId);
        result.PriceUnit.Should().Be("$/MWh");
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_SnapshotFields()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.Snapshot.Should().Be("today");
        result.SnapshotLabel.Should().Be("Today");
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_SixTermColumns()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.TermColumns.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_ThreeRows()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.Rows.Should().HaveCount(3);
        result.Rows.Select(r => r.Label).Should().BeEquivalentTo(["On-Peak", "Off-Peak", "All Hours"]);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_NoCellHasLowestPriceRange()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.Rows.SelectMany(r => r.Prices)
            .Should().NotContain(p => p.PriceRange == "LowestPrice",
                because: "range was renamed to BestValue to match the Figma legend");
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_BestValueCellsPresent()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.Rows.SelectMany(r => r.Prices)
            .Should().Contain(p => p.PriceRange == "BestValue");
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_MissingContractId_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?snapshot={ValidSnapshot}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_MissingSnapshot_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_UnknownContractId_Returns404()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/GetPricingIntelligence?contractId=UNKNOWN-999&snapshot=today");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_UnknownSnapshot_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot=yesterday");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_InvalidPriceUnit_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}&priceUnit=invalid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Billing_KwhPriceUnit_Returns200()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}&priceUnit=%24%2FkWh");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.PriceUnit.Should().Be("$/kWh");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/GetPricingTrend  (Billing)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingTrend_Billing_ValidParams_Returns200Json()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingTrend_Billing_ResponseShape()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingTrendResponse>(response);

        result.ContractId.Should().Be(ValidContractId);
        result.LoadType.Should().Be("All Hours");
        result.PriceUnit.Should().Be("$/MWh");
    }

    [Fact]
    public async Task GetPricingTrend_Billing_FourSeries()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingTrendResponse>(response);

        result.Series.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetPricingTrend_Billing_SeriesTermMonths()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingTrendResponse>(response);

        result.Series.Select(s => s.TermMonths)
            .Should().BeEquivalentTo([12, 24, 36, 48]);
    }

    [Fact]
    public async Task GetPricingTrend_Billing_EachSeriesHasNineDataPoints()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingTrendResponse>(response);

        result.Series.Should().OnlyContain(s => s.DataPoints.Count == 9);
    }

    [Fact]
    public async Task GetPricingTrend_Billing_ExactlyOneTodayPerSeries()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingTrendResponse>(response);

        foreach (var series in result.Series)
            series.DataPoints.Count(p => p.IsToday).Should().Be(1);
    }

    [Fact]
    public async Task GetPricingTrend_Billing_MissingContractId_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/GetPricingTrend");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingTrend_Billing_UnknownContractId_Returns404()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/GetPricingTrend?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/GetPricingSnapshotDetails  (Billing)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingSnapshotDetails_Billing_ValidParams_Returns200Json()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Billing_ResponseShape()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingSnapshotDetailsResponse>(response);

        result.ContractId.Should().Be(ValidContractId);
        result.PricingZone.Should().Be("ERCOT North");
        result.Product.Should().Be("Standard Fixed");
        result.Date.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Billing_TodaysLowestPrice()
    {
        SetBillingAuth();
        var response = await _client.GetAsync($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}");
        var result = await DeserializeAsync<PricingSnapshotDetailsResponse>(response);

        result.TodaysLowestPrice.TermMonths.Should().Be(36);
        result.TodaysLowestPrice.Price.Should().Be(45.10m);
        result.TodaysLowestPrice.PriceUnit.Should().Be("$/MWh");
        result.TodaysLowestPrice.ChangeVsYesterday.Should().Be(-1.20m);
        result.TodaysLowestPrice.ChangePercentVsYesterday.Should().Be(-2.6m);
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Billing_MissingContractId_Returns400()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/GetPricingSnapshotDetails");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Billing_UnknownContractId_Returns404()
    {
        SetBillingAuth();
        var response = await _client.GetAsync("/api/GetPricingSnapshotDetails?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/broker/renewals/GetPricingIntelligence  (Broker)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingIntelligence_Broker_ValidParams_Returns200Json()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_ResponseMatchesBillingResponse()
    {
        SetBrokerAuth();
        var brokerResponse = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var brokerResult = await DeserializeAsync<PricingIntelligenceResponse>(brokerResponse);

        SetBillingAuth();
        var billingResponse = await _client.GetAsync($"/api/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var billingResult = await DeserializeAsync<PricingIntelligenceResponse>(billingResponse);

        // Same service, same XML — responses must be identical
        brokerResult.ContractId.Should().Be(billingResult.ContractId);
        brokerResult.PriceUnit.Should().Be(billingResult.PriceUnit);
        brokerResult.Snapshot.Should().Be(billingResult.Snapshot);
        brokerResult.TermColumns.Should().HaveCount(billingResult.TermColumns.Count);
        brokerResult.Rows.Should().HaveCount(billingResult.Rows.Count);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_NoCellHasLowestPriceRange()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        result.Rows.SelectMany(r => r.Prices)
            .Should().NotContain(p => p.PriceRange == "LowestPrice");
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_MissingContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?snapshot={ValidSnapshot}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_MissingSnapshot_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_UnknownContractId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetPricingIntelligence?contractId=UNKNOWN-999&snapshot=today");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_UnknownSnapshot_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot=yesterday");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_InvalidPriceUnit_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}&priceUnit=invalid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingIntelligence_Broker_KwhPriceUnit_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingIntelligence?contractId={ValidContractId}&snapshot={ValidSnapshot}&priceUnit=%24%2FkWh");
        var result = await DeserializeAsync<PricingIntelligenceResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.PriceUnit.Should().Be("$/kWh");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/broker/renewals/GetPricingTrend  (Broker)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingTrend_Broker_ValidParams_Returns200Json()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingTrend?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingTrend_Broker_ResponseMatchesBillingEndpoint()
    {
        SetBrokerAuth();
        var brokerResult = await DeserializeAsync<PricingTrendResponse>(
            await _client.GetAsync($"/api/broker/renewals/GetPricingTrend?contractId={ValidContractId}"));

        SetBillingAuth();
        var billingResult = await DeserializeAsync<PricingTrendResponse>(
            await _client.GetAsync($"/api/GetPricingTrend?contractId={ValidContractId}"));

        brokerResult.ContractId.Should().Be(billingResult.ContractId);
        brokerResult.Series.Should().HaveCount(billingResult.Series.Count);
    }

    [Fact]
    public async Task GetPricingTrend_Broker_MissingContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetPricingTrend");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingTrend_Broker_UnknownContractId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetPricingTrend?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GET api/broker/renewals/GetPricingSnapshotDetails  (Broker)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPricingSnapshotDetails_Broker_ValidParams_Returns200Json()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/renewals/GetPricingSnapshotDetails?contractId={ValidContractId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Broker_ResponseMatchesBillingEndpoint()
    {
        SetBrokerAuth();
        var brokerResult = await DeserializeAsync<PricingSnapshotDetailsResponse>(
            await _client.GetAsync($"/api/broker/renewals/GetPricingSnapshotDetails?contractId={ValidContractId}"));

        SetBillingAuth();
        var billingResult = await DeserializeAsync<PricingSnapshotDetailsResponse>(
            await _client.GetAsync($"/api/GetPricingSnapshotDetails?contractId={ValidContractId}"));

        brokerResult.ContractId.Should().Be(billingResult.ContractId);
        brokerResult.PricingZone.Should().Be(billingResult.PricingZone);
        brokerResult.TodaysLowestPrice.Price.Should().Be(billingResult.TodaysLowestPrice.Price);
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Broker_MissingContractId_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetPricingSnapshotDetails");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPricingSnapshotDetails_Broker_UnknownContractId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/renewals/GetPricingSnapshotDetails?contractId=UNKNOWN-999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
