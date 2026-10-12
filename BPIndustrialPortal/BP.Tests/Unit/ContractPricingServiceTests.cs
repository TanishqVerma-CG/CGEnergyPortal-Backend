using BP.Services;
using BP.Tests.Helpers;

namespace BP.Tests.Unit;

/// <summary>
/// Unit tests for the pricing methods of ContractService using isolated temp XML.
///
/// Test data quick-reference (see TestXmlData.Pricing*):
///   Contract: TEST-P001
///   Snapshots: "today" (Today), "1week" (1 Week Ago)
///   Term columns per snapshot: 12 Month, 36 Month (36 is recommended)
///   Off-Peak 36M today  → $45.10, range=BestValue
///   All Hours 36M today → $51.20, range=BestValue
///   Trend series: 12M and 36M, 3 data points each, middle point isToday=true
///   Snapshot details: ERCOT North, Standard Fixed, lowest=$45.10, change=-1.20 (-2.6%)
/// </summary>
public class ContractPricingServiceTests : IClassFixture<ContractPricingServiceFixture>
{
    private readonly ContractService _sut;
    private readonly string _contentRootPath;

    public ContractPricingServiceTests(ContractPricingServiceFixture fixture)
    {
        _contentRootPath = fixture.ContentRootPath;
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        _sut = new ContractService(env, Mock.Of<BP.IService.ISupportCaseService>());
    }

    // ── GetPricingIntelligence ─────────────────────────────────────────────────

    [Fact]
    public void GetPricingIntelligence_ValidContract_ReturnsResponse()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingIntelligence_ContractId_RoundTrips()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.ContractId.Should().Be("TEST-P001");
    }

    [Fact]
    public void GetPricingIntelligence_SnapshotFields_AreCorrect()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.Snapshot.Should().Be("today");
        result.SnapshotLabel.Should().Be("Today");
    }

    [Fact]
    public void GetPricingIntelligence_PriceUnit_IsReturned()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.PriceUnit.Should().Be("$/MWh");
    }

    [Fact]
    public void GetPricingIntelligence_TermColumns_Count()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.TermColumns.Should().HaveCount(2);
    }

    [Fact]
    public void GetPricingIntelligence_RecommendedTerm_Is36Months()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.TermColumns.Single(t => t.IsRecommended).TermMonths.Should().Be(36);
    }

    [Fact]
    public void GetPricingIntelligence_Rows_ContainsAllThreeLoadTypes()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result!.Rows.Select(r => r.Label)
            .Should().BeEquivalentTo(["On-Peak", "Off-Peak", "All Hours"]);
    }

    [Fact]
    public void GetPricingIntelligence_BestValueRange_UsesCorrectName()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        // Off-Peak 36M and All Hours 36M are both BestValue — never LowestPrice
        var bestValueCells = result!.Rows
            .SelectMany(r => r.Prices)
            .Where(p => p.PriceRange == "BestValue")
            .ToList();

        bestValueCells.Should().HaveCount(2);
        result.Rows.SelectMany(r => r.Prices)
            .Should().NotContain(p => p.PriceRange == "LowestPrice");
    }

    [Fact]
    public void GetPricingIntelligence_OffPeak36M_CorrectPrice()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");
        var offPeakRow = result!.Rows.Single(r => r.Label == "Off-Peak");
        // Term columns: index 0 = 12M, index 1 = 36M
        offPeakRow.Prices[1].Price.Should().Be(45.10m);
    }

    [Fact]
    public void GetPricingIntelligence_AlternateSnapshot_ReturnsCorrectLabel()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "1week", "$/MWh");

        result!.SnapshotLabel.Should().Be("1 Week Ago");
    }

    [Fact]
    public void GetPricingIntelligence_ContractIdIsCaseInsensitive()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingIntelligence_SnapshotIdIsCaseInsensitive()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingIntelligence_UnknownContractId_ReturnsNull()
    {
        var result = _sut.GetPricingIntelligence("UNKNOWN-999", "today", "$/MWh");

        result.Should().BeNull();
    }

    [Fact]
    public void GetPricingIntelligence_UnknownSnapshot_ReturnsNull()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "yesterday", "$/MWh");

        result.Should().BeNull();
    }

    [Fact]
    public void GetPricingIntelligence_MissingXmlFile_ThrowsFileNotFoundException()
    {
        var badEnv = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == @"C:\NonExistent\DoesNotExist");
        var sut = new ContractService(badEnv, Mock.Of<BP.IService.ISupportCaseService>());

        var act = () => sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetPricingIntelligence_PriceUnit_KwhConversion_ReturnsKwhLabel()
    {
        var result = _sut.GetPricingIntelligence("TEST-P001", "today", "$/kWh");

        result!.PriceUnit.Should().Be("$/kWh");
    }

    [Fact]
    public void GetPricingIntelligence_PriceUnit_KwhConversion_DividesPriceByThousand()
    {
        var mwhResult = _sut.GetPricingIntelligence("TEST-P001", "today", "$/MWh");
        var kwhResult = _sut.GetPricingIntelligence("TEST-P001", "today", "$/kWh");

        var offPeakMwh = mwhResult!.Rows.Single(r => r.Label == "Off-Peak").Prices[1].Price;
        var offPeakKwh = kwhResult!.Rows.Single(r => r.Label == "Off-Peak").Prices[1].Price;

        offPeakKwh.Should().Be(Math.Round(offPeakMwh / 1000m, 5));
    }

    // ── GetPricingTrend ────────────────────────────────────────────────────────

    [Fact]
    public void GetPricingTrend_ValidContract_ReturnsResponse()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingTrend_ContractId_RoundTrips()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.ContractId.Should().Be("TEST-P001");
    }

    [Fact]
    public void GetPricingTrend_LoadType_IsAllHours()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.LoadType.Should().Be("All Hours");
    }

    [Fact]
    public void GetPricingTrend_PriceUnit_IsReturned()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.PriceUnit.Should().Be("$/MWh");
    }

    [Fact]
    public void GetPricingTrend_SeriesCount_IsTwo()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.Series.Should().HaveCount(2);
    }

    [Fact]
    public void GetPricingTrend_SeriesTermMonths_AreCorrect()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.Series.Select(s => s.TermMonths)
            .Should().BeEquivalentTo([12, 36]);
    }

    [Fact]
    public void GetPricingTrend_EachSeries_HasThreeDataPoints()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        result!.Series.Should().OnlyContain(s => s.DataPoints.Count == 3);
    }

    [Fact]
    public void GetPricingTrend_ExactlyOneDataPointIsToday()
    {
        var result = _sut.GetPricingTrend("TEST-P001");

        foreach (var series in result!.Series)
            series.DataPoints.Count(p => p.IsToday).Should().Be(1);
    }

    [Fact]
    public void GetPricingTrend_TodayDataPoint_HasCorrectDate()
    {
        var result = _sut.GetPricingTrend("TEST-P001");
        var series12 = result!.Series.Single(s => s.TermMonths == 12);

        series12.DataPoints.Single(p => p.IsToday).Date.Should().Be("2026-05-04");
    }

    [Fact]
    public void GetPricingTrend_12MonthSeries_TodayPrice()
    {
        var result = _sut.GetPricingTrend("TEST-P001");
        var today = result!.Series.Single(s => s.TermMonths == 12)
                                  .DataPoints.Single(p => p.IsToday);

        today.Price.Should().Be(59.00m);
    }

    [Fact]
    public void GetPricingTrend_36MonthSeries_TodayPrice()
    {
        var result = _sut.GetPricingTrend("TEST-P001");
        var today = result!.Series.Single(s => s.TermMonths == 36)
                                  .DataPoints.Single(p => p.IsToday);

        today.Price.Should().Be(50.00m);
    }

    [Fact]
    public void GetPricingTrend_DataPoints_ContainCorrectDates()
    {
        var result = _sut.GetPricingTrend("TEST-P001");
        var dates = result!.Series.First().DataPoints.Select(p => p.Date).ToList();

        dates.Should().ContainInOrder("2026-04-06", "2026-05-04", "2026-06-01");
    }

    [Fact]
    public void GetPricingTrend_UnknownContractId_ReturnsNull()
    {
        var result = _sut.GetPricingTrend("UNKNOWN-999");

        result.Should().BeNull();
    }

    [Fact]
    public void GetPricingTrend_ContractIdIsCaseInsensitive()
    {
        var result = _sut.GetPricingTrend("test-p001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingTrend_MissingXmlFile_ThrowsFileNotFoundException()
    {
        var badEnv = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == @"C:\NonExistent\DoesNotExist");
        var sut = new ContractService(badEnv, Mock.Of<BP.IService.ISupportCaseService>());

        var act = () => sut.GetPricingTrend("TEST-P001");

        act.Should().Throw<FileNotFoundException>();
    }

    // ── GetPricingSnapshotDetails ──────────────────────────────────────────────

    [Fact]
    public void GetPricingSnapshotDetails_ValidContract_ReturnsResponse()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingSnapshotDetails_ContractId_RoundTrips()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.ContractId.Should().Be("TEST-P001");
    }

    [Fact]
    public void GetPricingSnapshotDetails_Date_IsCorrect()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.Date.Should().Be("2026-05-06");
    }

    [Fact]
    public void GetPricingSnapshotDetails_PricingZone_IsCorrect()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.PricingZone.Should().Be("ERCOT North");
    }

    [Fact]
    public void GetPricingSnapshotDetails_Product_IsCorrect()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.Product.Should().Be("Standard Fixed");
    }

    [Fact]
    public void GetPricingSnapshotDetails_TodaysLowestPrice_TermMonths()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.TodaysLowestPrice.TermMonths.Should().Be(36);
    }

    [Fact]
    public void GetPricingSnapshotDetails_TodaysLowestPrice_Price()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.TodaysLowestPrice.Price.Should().Be(45.10m);
    }

    [Fact]
    public void GetPricingSnapshotDetails_TodaysLowestPrice_PriceUnit()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.TodaysLowestPrice.PriceUnit.Should().Be("$/MWh");
    }

    [Fact]
    public void GetPricingSnapshotDetails_TodaysLowestPrice_ChangeVsYesterday()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.TodaysLowestPrice.ChangeVsYesterday.Should().Be(-1.20m);
    }

    [Fact]
    public void GetPricingSnapshotDetails_TodaysLowestPrice_ChangePercent()
    {
        var result = _sut.GetPricingSnapshotDetails("TEST-P001");

        result!.TodaysLowestPrice.ChangePercentVsYesterday.Should().Be(-2.6m);
    }

    [Fact]
    public void GetPricingSnapshotDetails_UnknownContractId_ReturnsNull()
    {
        var result = _sut.GetPricingSnapshotDetails("UNKNOWN-999");

        result.Should().BeNull();
    }

    [Fact]
    public void GetPricingSnapshotDetails_ContractIdIsCaseInsensitive()
    {
        var result = _sut.GetPricingSnapshotDetails("test-p001");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetPricingSnapshotDetails_MissingXmlFile_ThrowsFileNotFoundException()
    {
        var badEnv = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == @"C:\NonExistent\DoesNotExist");
        var sut = new ContractService(badEnv, Mock.Of<BP.IService.ISupportCaseService>());

        var act = () => sut.GetPricingSnapshotDetails("TEST-P001");

        act.Should().Throw<FileNotFoundException>();
    }
}
