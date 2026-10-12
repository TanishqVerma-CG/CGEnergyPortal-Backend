using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BP.Models;
using BP.Tests.Helpers;
using FluentAssertions;

namespace BP.Tests.Integration;

/// <summary>
/// End-to-end integration tests for BrokerHomeController.
/// The real service, real XML data files, and real JWT middleware are all exercised.
/// A valid "Broker" JWT (signed with the app's key) is used for authenticated requests.
/// </summary>
public class BrokerHomeControllerTests : IClassFixture<BrokerApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public BrokerHomeControllerTests(BrokerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Auth helpers ───────────────────────────────────────────────────────────

    private void SetBrokerAuth() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateBrokerToken());

    private void ClearAuth() =>
        _client.DefaultRequestHeaders.Authorization = null;

    // ── Unauthenticated – all endpoints return 401 ────────────────────────────

    [Theory]
    [InlineData("/api/broker/home/GetSummaryMetrics")]
    [InlineData("/api/broker/home/GetRecentActivity")]
    [InlineData("/api/broker/home/GetRenewalWatchlist")]
    [InlineData("/api/broker/home/ExportRenewalWatchlist")]
    [InlineData("/api/broker/home/GetOpenSupportRequests")]
    public async Task Endpoint_WithoutToken_Returns401(string path)
    {
        ClearAuth();
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /GetSummaryMetrics ────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryMetrics_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetSummaryMetrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetSummaryMetrics_ResponseDeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetSummaryMetrics");
        var json = await response.Content.ReadAsStringAsync();
        var metrics = JsonSerializer.Deserialize<BrokerSummaryMetrics>(json, JsonOpts);

        metrics.Should().NotBeNull();
        metrics!.CustomersManaged.Total.Should().BeGreaterThan(0);
        metrics.ActiveContracts.Total.Should().BeGreaterThan(0);
        metrics.ActiveContracts.Fixed.Should().BeGreaterThan(0);
        metrics.ActiveContracts.Variable.Should().BeGreaterThan(0);
        metrics.UpcomingRenewals.AvgDaysToExpiry.Should().BeGreaterThan(0);
        metrics.MetersManaged.Total.Should().BeGreaterThan(0);
        metrics.AnnualUsage.TotalMWh.Should().BeGreaterThan(0);
        metrics.AnnualUsage.YoYChange.Should().NotBeNullOrEmpty();
    }

    // ── GET /GetRecentActivity ────────────────────────────────────────────────

    [Fact]
    public async Task GetRecentActivity_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetRecentActivity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetRecentActivity_ResponseDeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetRecentActivity");
        var json = await response.Content.ReadAsStringAsync();
        var activity = JsonSerializer.Deserialize<BrokerRecentActivity>(json, JsonOpts);

        activity.Should().NotBeNull();
        activity!.ContractAmendmentsSigned.Count.Should().BeGreaterThanOrEqualTo(0);
        activity.OpenSupportRequests.Count.Should().BeGreaterThanOrEqualTo(0);
        activity.UpcomingContractRenewals.DueWithinDays.Should().BeGreaterThan(0);
        activity.EstimatedRevenueAtRenewal.Amount.Should().BeGreaterThanOrEqualTo(0);
    }

    // ── GET /GetRenewalWatchlist ──────────────────────────────────────────────

    [Fact]
    public async Task GetRenewalWatchlist_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetRenewalWatchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRenewalWatchlist_ResponseDeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetRenewalWatchlist");
        var json = await response.Content.ReadAsStringAsync();
        var watchlist = JsonSerializer.Deserialize<RenewalWatchlistResponse>(json, JsonOpts);

        watchlist.Should().NotBeNull();
        watchlist!.TotalEntries.Should().BeGreaterThan(0);
        watchlist.Items.Should().NotBeEmpty();
        watchlist.Filters.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("0-15")]
    [InlineData("16-30")]
    [InlineData("31-60")]
    [InlineData("61-90")]
    [InlineData("all")]
    public async Task GetRenewalWatchlist_WithDaysFilter_Returns200(string filter)
    {
        SetBrokerAuth();
        var response = await _client.GetAsync($"/api/broker/home/GetRenewalWatchlist?daysFilter={filter}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RenewalWatchlistResponse>(json, JsonOpts);
        result!.Items.Should().NotBeNull();
    }

    [Theory]
    [InlineData("customer",      "asc")]
    [InlineData("customer",      "desc")]
    [InlineData("brokerMargin",  "asc")]
    [InlineData("brokerMargin",  "desc")]
    [InlineData("term",          "asc")]
    [InlineData("volume",        "asc")]
    [InlineData("daysRemaining", "asc")]
    [InlineData("daysRemaining", "desc")]
    public async Task GetRenewalWatchlist_WithSortParams_Returns200(string sortBy, string sortOrder)
    {
        SetBrokerAuth();
        var response = await _client.GetAsync(
            $"/api/broker/home/GetRenewalWatchlist?sortBy={sortBy}&sortOrder={sortOrder}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRenewalWatchlist_DefaultSort_ItemsAreOrderedByDaysRemainingAsc()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetRenewalWatchlist");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RenewalWatchlistResponse>(json, JsonOpts);

        var days = result!.Items.Select(i => i.DaysRemaining).ToList();
        days.Should().BeInAscendingOrder();
    }

    // ── GET /ExportRenewalWatchlist ───────────────────────────────────────────

    [Fact]
    public async Task ExportRenewalWatchlist_WithBrokerToken_ReturnsExcelFile()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/ExportRenewalWatchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var fileName = response.Content.Headers.ContentDisposition!.FileNameStar ?? string.Empty;
        fileName.Should().StartWith("renewal-watchlist-");
        fileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task ExportRenewalWatchlist_ExcelHasCorrectColumnsAndData()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/ExportRenewalWatchlist");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var ms = new MemoryStream(bytes);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        ws.Cell(1, 1).GetString().Should().Be("Customer");
        ws.Cell(1, 2).GetString().Should().Be("Broker Margin");
        ws.LastRowUsed()!.RowNumber().Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ExportRenewalWatchlist_WithDaysFilter_ReturnsFewerRowsThanAll()
    {
        SetBrokerAuth();

        var allBytes = await (await _client.GetAsync("/api/broker/home/ExportRenewalWatchlist?daysFilter=all")).Content.ReadAsByteArrayAsync();
        var filteredBytes = await (await _client.GetAsync("/api/broker/home/ExportRenewalWatchlist?daysFilter=0-15")).Content.ReadAsByteArrayAsync();

        using var allMs = new MemoryStream(allBytes);
        using var filteredMs = new MemoryStream(filteredBytes);
        using var allWb = new ClosedXML.Excel.XLWorkbook(allMs);
        using var filteredWb = new ClosedXML.Excel.XLWorkbook(filteredMs);

        var allRows = allWb.Worksheet(1).LastRowUsed()!.RowNumber();
        var filteredRows = filteredWb.Worksheet(1).LastRowUsed()!.RowNumber();

        filteredRows.Should().BeLessThan(allRows);
    }

    // ── GET /GetOpenSupportRequests ───────────────────────────────────────────

    [Fact]
    public async Task GetOpenSupportRequests_WithBrokerToken_Returns200()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetOpenSupportRequests");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOpenSupportRequests_ResponseDeserializesCorrectly()
    {
        SetBrokerAuth();
        var response = await _client.GetAsync("/api/broker/home/GetOpenSupportRequests");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BrokerOpenSupportRequestsResponse>(json, JsonOpts);

        result.Should().NotBeNull();
        result!.TotalEntries.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(i =>
        {
            i.Customer.Should().NotBeNullOrEmpty();
            i.RequestType.Should().NotBeNullOrEmpty();
            i.Priority.Should().NotBeNullOrEmpty();
            i.CreatedOn.Should().NotBeNullOrEmpty();
        });
    }

    [Theory]
    [InlineData("customer",    "asc")]
    [InlineData("requestType", "asc")]
    [InlineData("priority",    "asc")]
    [InlineData("priority",    "desc")]
    [InlineData("createdOn",   "asc")]
    [InlineData("createdOn",   "desc")]
    public async Task GetOpenSupportRequests_WithSortParams_Returns200(string sortBy, string sortOrder)
    {
        SetBrokerAuth();
        var response = await _client.GetAsync(
            $"/api/broker/home/GetOpenSupportRequests?sortBy={sortBy}&sortOrder={sortOrder}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
