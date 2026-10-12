using BP.Services;
using BP.Tests.Helpers;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

public class BrokerDealsServiceTests : IClassFixture<BrokerDealsServiceFixture>
{
    private readonly BrokerDealsService _sut;

    public BrokerDealsServiceTests(BrokerDealsServiceFixture fixture)
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        _sut = new BrokerDealsService(env);
    }

    // ── GetMyDeals ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetMyDeals_NoFilter_ReturnsAllDeals()
    {
        var result = _sut.GetMyDeals();

        result.TotalEntries.Should().Be(3);
        result.Deals.Should().HaveCount(3);
    }

    [Fact]
    public void GetMyDeals_FilterByAgencyId_ReturnsOnlyThatAgencyDeals()
    {
        // A1 has Alpha Corp + Gamma Ltd (2 deals)
        var result = _sut.GetMyDeals("A1");

        result.TotalEntries.Should().Be(2);
        result.Deals.Should().AllSatisfy(d => d.CompanyName.Should().BeOneOf("Alpha Corp", "Gamma Ltd"));
    }

    [Fact]
    public void GetMyDeals_FilterByAgencyId_ExcludesOtherAgencies()
    {
        var result = _sut.GetMyDeals("A2");

        result.TotalEntries.Should().Be(1);
        result.Deals.Single().CompanyName.Should().Be("Beta Inc");
    }

    [Fact]
    public void GetMyDeals_UnknownAgencyId_ReturnsEmpty()
    {
        var result = _sut.GetMyDeals("UNKNOWN");

        result.TotalEntries.Should().Be(0);
        result.Deals.Should().BeEmpty();
    }

    [Fact]
    public void GetMyDeals_NullAgencyId_ReturnsAll()
    {
        var result = _sut.GetMyDeals(null);

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetMyDeals_AllFieldsMapped()
    {
        var result = _sut.GetMyDeals("A1");
        var alpha = result.Deals.First(d => d.CompanyName == "Alpha Corp");

        alpha.ContractNumber.Should().Be("C-ALPHA-001");
        alpha.ContractStartDate.Should().Be("01/01/2026");
        alpha.Term.Should().Be(12);
        alpha.CommissionPlan.Should().Be("6 Months Upfront");
        alpha.TermVolume.Should().Be(500.00m);
        alpha.UnitFee.Should().Be(0.00500m);
        alpha.CurrentPaymentDate.Should().Be("NA");
        alpha.CurrentPayment.Should().Be(0.00m);
    }

    [Fact]
    public void GetMyDeals_DealWithCurrentPayment_MappedCorrectly()
    {
        var result = _sut.GetMyDeals("A1");
        var gamma = result.Deals.First(d => d.CompanyName == "Gamma Ltd");

        gamma.CurrentPaymentDate.Should().Be("07/01/2025");
        gamma.CurrentPayment.Should().Be(10.00m);
    }

    // ── ExportDeals ────────────────────────────────────────────────────────────

    [Fact]
    public void ExportDeals_NoFilter_ReturnsNonEmptyByteArray()
    {
        var bytes = _sut.ExportDeals();

        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExportDeals_FilteredByAgency_ReturnsOnlyThatAgencyRows()
    {
        var bytes = _sut.ExportDeals("A1");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("My Deals");

        // 1 header + 2 data rows (Alpha Corp + Gamma Ltd)
        ws.LastRowUsed()!.RowNumber().Should().Be(3);
    }

    [Fact]
    public void ExportDeals_ReturnsValidXlsxFile()
    {
        var bytes = _sut.ExportDeals();

        using var stream = new MemoryStream(bytes);
        var act = () => new XLWorkbook(stream);
        act.Should().NotThrow();
    }

    [Fact]
    public void ExportDeals_WorksheetContainsCorrectRowCount()
    {
        var bytes = _sut.ExportDeals();

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("My Deals");

        // 1 header row + 3 data rows
        ws.LastRowUsed()!.RowNumber().Should().Be(4);
    }

    [Fact]
    public void ExportDeals_HeaderRowHasCorrectColumnNames()
    {
        var bytes = _sut.ExportDeals();

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("My Deals");

        ws.Cell(1, 1).GetValue<string>().Should().Be("Company Name");
        ws.Cell(1, 2).GetValue<string>().Should().Be("Contract Number");
        ws.Cell(1, 9).GetValue<string>().Should().Be("Current Payment");
    }

    // ── Missing file ───────────────────────────────────────────────────────────

    [Fact]
    public void GetMyDeals_MissingFile_ThrowsFileNotFoundException()
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == Path.Combine(Path.GetTempPath(), "nonexistent-bp"));
        var sut = new BrokerDealsService(env);

        var act = () => sut.GetMyDeals();

        act.Should().Throw<FileNotFoundException>();
    }
}
