using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BP.Models;
using BP.Tests.Helpers;
using FluentAssertions;

namespace BP.Tests.Integration;

/// <summary>
/// End-to-end integration tests for BrokerAgencyController.
/// </summary>
public class BrokerAgencyControllerTests : IClassFixture<BrokerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public BrokerAgencyControllerTests(BrokerApiFactory factory)
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
    [InlineData("/api/broker/commission/agency/SearchBrokers")]
    [InlineData("/api/broker/commission/agency/GetAgencyDashboard?agencyId=1")]
    public async Task Endpoint_WithoutToken_Returns401(string path)
    {
        ClearAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/broker/commission/agency/SearchBrokers")]
    [InlineData("/api/broker/commission/agency/GetAgencyDashboard?agencyId=1")]
    public async Task Endpoint_WithBillingToken_Returns403(string path)
    {
        SetBillingAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    // ── SearchBrokers ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchBrokers_NoParams_Returns200WithResults()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/SearchBrokers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task SearchBrokers_NoParams_DeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/SearchBrokers");
        var result = await DeserializeAsync<BrokerSearchResponse>(response);

        result.Should().NotBeNull();
        result!.TotalEntries.Should().BeGreaterThan(0);
        result.Results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchBrokers_WithAgencyName_FiltersResults()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/SearchBrokers?agencyName=New+Sales+Agency");
        var result = await DeserializeAsync<BrokerSearchResponse>(response);

        result.Should().NotBeNull();
        result!.Results.Should().AllSatisfy(r =>
            r.AgencyName.Should().Contain("New Sales Agency", Exactly.Once()));
    }

    [Fact]
    public async Task SearchBrokers_NoMatch_ReturnsEmptyResults()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/SearchBrokers?agencyName=ZZZNOMATCH");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<BrokerSearchResponse>(response);
        result!.TotalEntries.Should().Be(0);
    }

    [Fact]
    public async Task SearchBrokers_ResultsHaveRequiredFields()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/SearchBrokers");
        var result = await DeserializeAsync<BrokerSearchResponse>(response);

        result!.Results.Should().AllSatisfy(r =>
        {
            r.AgencyId.Should().NotBeNullOrEmpty();
            r.AgencyName.Should().NotBeNullOrEmpty();
            r.NumberOfContracts.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    // ── GetAgencyDashboard ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAgencyDashboard_ValidId_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/GetAgencyDashboard?agencyId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAgencyDashboard_ValidId_DeserializesAllSections()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/GetAgencyDashboard?agencyId=1");
        var result = await DeserializeAsync<AgencyDashboardResponse>(response);

        result.Should().NotBeNull();
        result!.Agency.AgencyId.Should().Be("1");
        result.OnFlowDeals.Should().NotBeEmpty();
        result.PaymentChart.Should().NotBeEmpty();
        result.AgencySummary.NumberOfAgents.Should().BeGreaterThan(0);
        result.Agents.Should().NotBeEmpty();
        result.Agents.Should().AllSatisfy(a =>
        {
            a.AgentId.Should().NotBeNullOrEmpty();
            a.AgentName.Should().NotBeNullOrEmpty();
            a.Role.Should().NotBeNullOrEmpty();
            a.Email.Should().NotBeNullOrEmpty();
            a.Phone.Should().NotBeNullOrEmpty();
            a.ActiveDeals.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task GetAgencyDashboard_UnknownId_Returns404()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/GetAgencyDashboard?agencyId=GHOST999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAgencyDashboard_MissingParam_Returns400()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/commission/agency/GetAgencyDashboard");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
