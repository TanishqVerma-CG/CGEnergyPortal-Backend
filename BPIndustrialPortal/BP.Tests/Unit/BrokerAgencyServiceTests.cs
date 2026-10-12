using BP.Services;
using BP.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

public class BrokerAgencyServiceTests : IClassFixture<BrokerAgencyServiceFixture>
{
    private readonly BrokerAgencyService _sut;

    public BrokerAgencyServiceTests(BrokerAgencyServiceFixture fixture)
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        _sut = new BrokerAgencyService(env);
    }

    // ── SearchBrokers ──────────────────────────────────────────────────────────

    [Fact]
    public void SearchBrokers_NoFilter_ReturnsAllAgencies()
    {
        var result = _sut.SearchBrokers(null, null);

        result.TotalEntries.Should().Be(3);
        result.Results.Should().HaveCount(3);
    }

    [Fact]
    public void SearchBrokers_ByAgencyName_FiltersCorrectly()
    {
        var result = _sut.SearchBrokers(null, "Apex");

        // Apex Energy + Apex Solutions
        result.TotalEntries.Should().Be(2);
        result.Results.Should().OnlyContain(r => r.AgencyName.Contains("Apex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchBrokers_ByContactName_FiltersCorrectly()
    {
        var result = _sut.SearchBrokers("beta", null);

        result.TotalEntries.Should().Be(1);
        result.Results.Single().AgencyName.Should().Be("Beta Brokers");
    }

    [Fact]
    public void SearchBrokers_BothFilters_AppliedWithAnd()
    {
        // agencyName="Apex" matches A1 and A3; contactName="apex" only matches A1
        var result = _sut.SearchBrokers("apex", "Apex");

        result.TotalEntries.Should().Be(1);
        result.Results.Single().AgencyId.Should().Be("A1");
    }

    [Fact]
    public void SearchBrokers_CaseInsensitiveAgencyName()
    {
        var result = _sut.SearchBrokers(null, "BETA BROKERS");

        result.TotalEntries.Should().Be(1);
    }

    [Fact]
    public void SearchBrokers_CaseInsensitiveContactName()
    {
        var result = _sut.SearchBrokers("APEX CONTACT", null);

        result.TotalEntries.Should().Be(1);
    }

    [Fact]
    public void SearchBrokers_NoMatch_ReturnsEmpty()
    {
        var result = _sut.SearchBrokers(null, "ZZZ_NONEXISTENT");

        result.TotalEntries.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public void SearchBrokers_ResultContainsAllFields()
    {
        var result = _sut.SearchBrokers(null, "Beta Brokers");
        var r = result.Results.Single();

        r.AgencyId.Should().Be("A2");
        r.AgencyName.Should().Be("Beta Brokers");
        r.ContactName.Should().Be("beta contact");
        r.NumberOfContracts.Should().Be(5);
    }

    // ── GetAgencyDashboard ─────────────────────────────────────────────────────

    [Fact]
    public void GetAgencyDashboard_ValidId_ReturnsResponse()
    {
        var result = _sut.GetAgencyDashboard("A1");

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetAgencyDashboard_AgencyDetailMapped()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

        result.Agency.AgencyId.Should().Be("A1");
        result.Agency.Name.Should().Be("Apex Energy");
        result.Agency.Address.City.Should().Be("Austin");
        result.Agency.Address.State.Should().Be("TX");
        result.Agency.Address.Zip.Should().Be("78701");
    }

    [Fact]
    public void GetAgencyDashboard_OnFlowDeals_CorrectCount()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

        result.OnFlowDeals.Should().HaveCount(2);
        result.OnFlowDeals.Should().AllSatisfy(p => p.Month.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void GetAgencyDashboard_PaymentChart_HasForecastedPoint()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

        result.PaymentChart.Should().Contain(p => p.IsForecasted);
        result.PaymentChart.Should().Contain(p => !p.IsForecasted);
    }

    [Fact]
    public void GetAgencyDashboard_AgencySummaryMapped()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

        result.AgencySummary.NumberOfAgents.Should().Be(3);
        result.AgencySummary.NumberOfDeals.Should().Be(10);
        result.AgencySummary.CurrentPayment.Should().Be(500.00m);
        result.AgencySummary.ForecastedPayment.Should().Be(600.00m);
        result.AgencySummary.YearToDatePayment.Should().Be(900.00m);
    }

    [Fact]
    public void GetAgencyDashboard_AgentsMapped()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

        result.Agents.Should().HaveCount(2);
        result.Agents.Should().Contain(a => a.AgentName == "Alice");
        result.Agents.Should().Contain(a => a.AgentName == "Bob");
    }

    [Fact]
    public void GetAgencyDashboard_AgentDetailFieldsMapped()
    {
        var result = _sut.GetAgencyDashboard("A1")!;
        var alice = result.Agents.First(a => a.AgentName == "Alice");

        alice.AgentId.Should().Be("AG01");
        alice.Role.Should().Be("Senior Agent");
        alice.Email.Should().Be("alice@apexenergy.example.com");
        alice.Phone.Should().Be("555-001-0001");
        alice.ActiveDeals.Should().Be(3);
    }

    [Fact]
    public void GetAgencyDashboard_AllAgentsHaveRequiredFields()
    {
        var result = _sut.GetAgencyDashboard("A1")!;

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
    public void GetAgencyDashboard_InvalidId_ReturnsNull()
    {
        var result = _sut.GetAgencyDashboard("NONEXISTENT");

        result.Should().BeNull();
    }

    // ── Missing file ───────────────────────────────────────────────────────────

    [Fact]
    public void SearchBrokers_MissingFile_ThrowsFileNotFoundException()
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == Path.Combine(Path.GetTempPath(), "nonexistent-bp"));
        var sut = new BrokerAgencyService(env);

        var act = () => sut.SearchBrokers(null, null);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetAgencyDashboard_MissingFile_ThrowsFileNotFoundException()
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == Path.Combine(Path.GetTempPath(), "nonexistent-bp"));
        var sut = new BrokerAgencyService(env);

        var act = () => sut.GetAgencyDashboard("A1");

        act.Should().Throw<FileNotFoundException>();
    }
}
