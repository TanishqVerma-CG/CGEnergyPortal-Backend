using BP.Services;
using BP.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace BP.Tests.Unit;

/// <summary>
/// Unit tests for BrokerHomeService. Each test uses an isolated temp directory
/// seeded with known XML data (see TestXmlData).
///
/// Test data quick-reference:
///   Renewal watchlist (5 items):
///     Epsilon LLC  – margin 0.075 – term 12 – vol  6000 – days  5  (0-15)
///     Alpha Corp   – margin 0.050 – term 12 – vol  5000 – days 10  (0-15)
///     Beta Inc     – margin 0.025 – term 24 – vol  8000 – days 20  (16-30)
///     Delta Ltd    – margin 0.100 – term 36 – vol  3000 – days 45  (31-60)
///     Gamma Co     – margin 0.030 – term 18 – vol 12000 – days 75  (61-90)
///
///   Open support requests (4 items):
///     Acme Inc  – Billing Dispute    – HIGH   – 5/10/2026
///     Delta Co  – Billing Dispute    – HIGH   – 5/12/2026
///     Alpha Ltd – Other              – MEDIUM – 5/8/2026
///     Beta Corp – Information Request– LOW    – 5/5/2026
/// </summary>
public class BrokerHomeServiceTests : IClassFixture<BrokerHomeServiceFixture>
{
    private readonly BrokerHomeService _sut;

    public BrokerHomeServiceTests(BrokerHomeServiceFixture fixture)
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        _sut = new BrokerHomeService(env);
    }

    // ── GetSummaryMetrics ──────────────────────────────────────────────────────

    [Fact]
    public void GetSummaryMetrics_ReturnsCorrectCustomersManaged()
    {
        var result = _sut.GetSummaryMetrics();

        result.CustomersManaged.Total.Should().Be(50);
        result.CustomersManaged.NewThisMonth.Should().Be(3);
        result.CustomersManaged.NewThisMonthLabel.Should().Be("+3 this month");
        result.CustomersManaged.Churned.Should().Be(1);
        result.CustomersManaged.ChurnedLabel.Should().Be("1 churned");
    }

    [Fact]
    public void GetSummaryMetrics_ReturnsCorrectActiveContracts()
    {
        var result = _sut.GetSummaryMetrics();

        result.ActiveContracts.Total.Should().Be(100);
        result.ActiveContracts.Fixed.Should().Be(60);
        result.ActiveContracts.Variable.Should().Be(40);
        result.ActiveContracts.BreakdownLabel.Should().Be("Fixed 60 · Var 40");
    }

    [Fact]
    public void GetSummaryMetrics_ReturnsCorrectUpcomingRenewals()
    {
        var result = _sut.GetSummaryMetrics();

        result.UpcomingRenewals.Total.Should().Be(10);
        result.UpcomingRenewals.Pending.Should().Be(2);
        result.UpcomingRenewals.PendingLabel.Should().Be("Pending · 2");
        result.UpcomingRenewals.AvgDaysToExpiry.Should().Be(20);
        result.UpcomingRenewals.AvgExpiryLabel.Should().Be("Avg. 20d expiry");
    }

    [Fact]
    public void GetSummaryMetrics_ReturnsCorrectMetersManaged()
    {
        var result = _sut.GetSummaryMetrics();

        result.MetersManaged.Total.Should().Be(200);
        result.MetersManaged.Decreased.Should().Be(120);
        result.MetersManaged.Increased.Should().Be(80);
    }

    [Fact]
    public void GetSummaryMetrics_ReturnsCorrectAnnualUsage()
    {
        var result = _sut.GetSummaryMetrics();

        result.AnnualUsage.TotalMWh.Should().Be(45000m);
        result.AnnualUsage.YoYChange.Should().Be("+3.0%");
        result.AnnualUsage.YoYLabel.Should().Be("+3.0% YoY");
    }

    [Fact]
    public void GetSummaryMetrics_MissingFile_ThrowsFileNotFoundException()
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == Path.GetTempPath() + "/nonexistent");
        var service = new BrokerHomeService(env);

        var act = () => service.GetSummaryMetrics();

        act.Should().Throw<FileNotFoundException>();
    }

    // ── GetRecentActivity ──────────────────────────────────────────────────────

    [Fact]
    public void GetRecentActivity_ReturnsCorrectContractAmendments()
    {
        var result = _sut.GetRecentActivity();

        result.ContractAmendmentsSigned.Count.Should().Be(5);
        result.ContractAmendmentsSigned.Label.Should().Be("Across all customers");
    }

    [Fact]
    public void GetRecentActivity_ReturnsCorrectOpenSupportRequests()
    {
        var result = _sut.GetRecentActivity();

        result.OpenSupportRequests.Count.Should().Be(8);
        result.OpenSupportRequests.HighPriority.Should().Be(3);
        result.OpenSupportRequests.Label.Should().Be("3 High priority");
    }

    [Fact]
    public void GetRecentActivity_ReturnsCorrectUpcomingRenewals()
    {
        var result = _sut.GetRecentActivity();

        result.UpcomingContractRenewals.Count.Should().Be(10);
        result.UpcomingContractRenewals.DueWithinDays.Should().Be(90);
        result.UpcomingContractRenewals.Label.Should().Be("Due within the next 90 days");
    }

    [Fact]
    public void GetRecentActivity_ReturnsCorrectEstimatedRevenue()
    {
        var result = _sut.GetRecentActivity();

        result.EstimatedRevenueAtRenewal.Amount.Should().Be(150000m);
        result.EstimatedRevenueAtRenewal.RenewalCount.Should().Be(10);
        result.EstimatedRevenueAtRenewal.Label.Should().Be("Across 10 upcoming renewals");
    }

    // ── GetRenewalWatchlist – filtering ───────────────────────────────────────

    [Fact]
    public void GetRenewalWatchlist_NoFilter_ReturnsAllItems()
    {
        var result = _sut.GetRenewalWatchlist("all", "daysRemaining", "asc");

        result.TotalEntries.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("0-15", 2)]
    [InlineData("16-30", 1)]
    [InlineData("31-60", 1)]
    [InlineData("61-90", 1)]
    [InlineData("all", 5)]
    public void GetRenewalWatchlist_DaysFilter_ReturnsCorrectCount(string filter, int expectedCount)
    {
        var result = _sut.GetRenewalWatchlist(filter, "daysRemaining", "asc");

        result.TotalEntries.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);
    }

    [Fact]
    public void GetRenewalWatchlist_Filter015_ContainsOnlyItemsIn015Range()
    {
        var result = _sut.GetRenewalWatchlist("0-15", "daysRemaining", "asc");

        result.Items.Should().AllSatisfy(i => i.DaysRemaining.Should().BeInRange(0, 15));
        result.Items.Select(i => i.Customer).Should().BeEquivalentTo(["Epsilon LLC", "Alpha Corp"]);
    }

    [Fact]
    public void GetRenewalWatchlist_FiltersResponse_ContainsFiveOptionsWithCorrectCounts()
    {
        var result = _sut.GetRenewalWatchlist("all", "daysRemaining", "asc");

        result.Filters.Should().HaveCount(5);
        result.Filters.First(f => f.Value == "all").Count.Should().Be(5);
        result.Filters.First(f => f.Value == "0-15").Count.Should().Be(2);
        result.Filters.First(f => f.Value == "16-30").Count.Should().Be(1);
        result.Filters.First(f => f.Value == "31-60").Count.Should().Be(1);
        result.Filters.First(f => f.Value == "61-90").Count.Should().Be(1);
    }

    // ── GetRenewalWatchlist – sorting ─────────────────────────────────────────

    [Theory]
    [InlineData("daysRemaining", "asc",  "Epsilon LLC", "Gamma Co")]
    [InlineData("daysRemaining", "desc", "Gamma Co",    "Epsilon LLC")]
    [InlineData("customer",      "asc",  "Alpha Corp",  "Gamma Co")]
    [InlineData("customer",      "desc", "Gamma Co",    "Alpha Corp")]
    [InlineData("brokerMargin",  "asc",  "Beta Inc",    "Delta Ltd")]
    [InlineData("brokerMargin",  "desc", "Delta Ltd",   "Beta Inc")]
    [InlineData("volume",        "asc",  "Delta Ltd",   "Gamma Co")]
    [InlineData("volume",        "desc", "Gamma Co",    "Delta Ltd")]
    public void GetRenewalWatchlist_SortParams_ProduceCorrectFirstAndLastItem(
        string sortBy, string sortOrder, string expectedFirst, string expectedLast)
    {
        var result = _sut.GetRenewalWatchlist("all", sortBy, sortOrder);

        result.Items.First().Customer.Should().Be(expectedFirst);
        result.Items.Last().Customer.Should().Be(expectedLast);
    }

    [Fact]
    public void GetRenewalWatchlist_SortByTermAsc_FirstItemHasLowestTermMonths()
    {
        var result = _sut.GetRenewalWatchlist("all", "term", "asc");

        result.Items.First().TermMonths.Should().Be(12);
        result.Items.Last().TermMonths.Should().Be(36);
    }

    // ── ExportRenewalWatchlist ─────────────────────────────────────────────────

    [Fact]
    public void ExportRenewalWatchlist_NoFilter_ReturnsExcelWithHeaderAndAllDataRows()
    {
        var bytes = _sut.ExportRenewalWatchlist("all");

        using var ms = new MemoryStream(bytes);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        ws.Name.Should().Be("Renewal Watchlist");
        // 1 header + 5 data rows
        ws.LastRowUsed()!.RowNumber().Should().Be(6);
    }

    [Fact]
    public void ExportRenewalWatchlist_Filter015_ReturnsExcelWithFilteredDataRows()
    {
        var bytes = _sut.ExportRenewalWatchlist("0-15");

        using var ms = new MemoryStream(bytes);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        // 1 header + 2 data rows (Epsilon LLC day 5, Alpha Corp day 10)
        ws.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void ExportRenewalWatchlist_ExcelHasCorrectColumnHeaders()
    {
        var bytes = _sut.ExportRenewalWatchlist("all");

        using var ms = new MemoryStream(bytes);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        ws.Cell(1, 1).GetString().Should().Be("Customer");
        ws.Cell(1, 2).GetString().Should().Be("Broker Margin");
        ws.Cell(1, 3).GetString().Should().Be("Term (Month)");
        ws.Cell(1, 4).GetString().Should().Be("Volume (MWh)");
        ws.Cell(1, 5).GetString().Should().Be("Days Remaining");
    }

    [Fact]
    public void ExportRenewalWatchlist_HeaderRow_HasBpGreenBackground()
    {
        var bytes = _sut.ExportRenewalWatchlist("all");

        using var ms = new MemoryStream(bytes);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        // BP Green = #007A33
        var expectedGreen = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x7A, 0x33);
        ws.Cell(1, 1).Style.Fill.BackgroundColor.Color.ToArgb()
            .Should().Be(expectedGreen.ToArgb());
        ws.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        var expectedWhite = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        ws.Cell(1, 1).Style.Font.FontColor.Color.ToArgb().Should().Be(expectedWhite.ToArgb());
    }

    // ── GetOpenSupportRequests – sorting ──────────────────────────────────────

    [Fact]
    public void GetOpenSupportRequests_DefaultSort_ReturnsMostRecentFirst()
    {
        // default: createdOn desc
        var result = _sut.GetOpenSupportRequests("createdOn", "desc");

        result.Items.First().CreatedOn.Should().Be("5/12/2026");  // Delta Co
        result.Items.Last().CreatedOn.Should().Be("5/5/2026");    // Beta Corp
    }

    [Fact]
    public void GetOpenSupportRequests_SortByPriorityAsc_OrdersHighBeforeLow()
    {
        var result = _sut.GetOpenSupportRequests("priority", "asc");

        result.Items.First().Priority.Should().Be("HIGH");
        result.Items.Last().Priority.Should().Be("LOW");
    }

    [Fact]
    public void GetOpenSupportRequests_SortByCustomerAsc_ReturnsAlphabeticalOrder()
    {
        var result = _sut.GetOpenSupportRequests("customer", "asc");

        result.Items.Select(i => i.Customer).Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public void GetOpenSupportRequests_SortByRequestTypeAsc_ReturnsAlphabeticalOrder()
    {
        var result = _sut.GetOpenSupportRequests("requestType", "asc");

        result.Items.Select(i => i.RequestType).Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public void GetOpenSupportRequests_ReturnsTotalEntries()
    {
        var result = _sut.GetOpenSupportRequests("createdOn", "desc");

        result.TotalEntries.Should().Be(4);
        result.Items.Should().HaveCount(4);
    }
}
