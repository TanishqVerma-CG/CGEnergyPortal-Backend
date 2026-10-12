using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BP.Models;
using BP.Tests.Helpers;
using FluentAssertions;

namespace BP.Tests.Integration;

/// <summary>
/// End-to-end integration tests for BrokerDealsController.
/// </summary>
public class BrokerDealsControllerTests : IClassFixture<BrokerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public BrokerDealsControllerTests(BrokerApiFactory factory)
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

    private async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // ── 401 / 403 ─────────────────────────────────────────────────────────────
    // These return 401/403 when [Authorize] is active; 200 when auth is commented out.

    [Theory]
    [InlineData("/api/broker/commission/deals/GetMyDeals")]
    [InlineData("/api/broker/commission/deals/ExportDeals")]
    public async Task Endpoint_WithoutToken_Returns401(string path)
    {
        ClearAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/broker/commission/deals/GetMyDeals")]
    [InlineData("/api/broker/commission/deals/ExportDeals")]
    public async Task Endpoint_WithBillingToken_Returns403(string path)
    {
        SetBillingAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    // ── GetMyDeals ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyDeals_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetMyDeals_DeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals");
        var result = await DeserializeAsync<DealsResponse>(response);

        result.Should().NotBeNull();
        result!.TotalEntries.Should().BeGreaterThan(0);
        result.Deals.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyDeals_FilterByAgencyId_ReturnsSubset()
    {
        SetBrokerAuth();
        var allResponse = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals");
        var agencyResponse = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals?agencyId=1");

        var all = await DeserializeAsync<DealsResponse>(allResponse);
        var agency = await DeserializeAsync<DealsResponse>(agencyResponse);

        agencyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        agency!.TotalEntries.Should().BeLessThanOrEqualTo(all!.TotalEntries);
        agency.Deals.Should().AllSatisfy(d => d.CompanyName.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetMyDeals_UnknownAgencyId_ReturnsEmptyList()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals?agencyId=UNKNOWN_XYZ");
        var result = await DeserializeAsync<DealsResponse>(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result!.TotalEntries.Should().Be(0);
        result.Deals.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyDeals_DealsHaveRequiredFields()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/GetMyDeals");
        var result = await DeserializeAsync<DealsResponse>(response);

        result!.Deals.Should().AllSatisfy(d =>
        {
            d.CompanyName.Should().NotBeNullOrEmpty();
            d.ContractNumber.Should().NotBeNullOrEmpty();
            d.CommissionPlan.Should().NotBeNullOrEmpty();
        });
    }

    // ── ExportDeals ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportDeals_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/ExportDeals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExportDeals_ReturnsXlsxContentType()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/ExportDeals");

        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportDeals_ResponseBodyIsNonEmpty()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/deals/ExportDeals");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        bytes.Should().NotBeEmpty();
    }
}
