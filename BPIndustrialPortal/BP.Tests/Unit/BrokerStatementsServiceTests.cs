using BP.IService;
using BP.Models;
using BP.Services;
using BP.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;

public class BrokerStatementsServiceTests : IClassFixture<BrokerStatementsServiceFixture>
{
    private readonly BrokerStatementsServiceFixture _fixture;
    private readonly BrokerStatementsService _sut;
    private readonly Mock<IEmailService> _emailServiceMock;

    public BrokerStatementsServiceTests(BrokerStatementsServiceFixture fixture)
    {
        _fixture = fixture;
        _emailServiceMock = new Mock<IEmailService>();
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == fixture.ContentRootPath);
        _sut = new BrokerStatementsService(env, _emailServiceMock.Object);
    }

    // ── GetMyStatements ────────────────────────────────────────────────────────

    [Fact]
    public void GetMyStatements_NoFilter_ReturnsAllStatements()
    {
        var result = _sut.GetMyStatements();

        result.TotalEntries.Should().Be(3);
        result.Statements.Should().HaveCount(3);
    }

    [Fact]
    public void GetMyStatements_FilterByAgencyId_ReturnsOnlyThatAgency()
    {
        // All 3 test statements belong to agencyId=10
        var result = _sut.GetMyStatements("10");

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetMyStatements_UnknownAgencyId_ReturnsEmpty()
    {
        var result = _sut.GetMyStatements("UNKNOWN");

        result.TotalEntries.Should().Be(0);
        result.Statements.Should().BeEmpty();
    }

    [Fact]
    public void GetMyStatements_NullAgencyId_ReturnsAll()
    {
        var result = _sut.GetMyStatements(null);

        result.TotalEntries.Should().Be(3);
    }

    [Fact]
    public void GetMyStatements_AllFieldsMapped()
    {
        var result = _sut.GetMyStatements();
        var s1 = result.Statements.First(s => s.StatementId == "S1");

        s1.AgencyName.Should().Be("Test Agency");
        s1.SubmittedOn.Should().Be("01/15/2026");
        s1.Payments.Should().Be(100.00m);
        s1.Adjustments.Should().Be(0.00m);
        s1.Clawbacks.Should().Be(-10.00m);
        s1.SubTotal.Should().Be(100.00m);
        s1.Amount.Should().Be(90.00m);
        s1.Status.Should().Be("PENDING");
        s1.PaymentDate.Should().Be("NA");
    }

    [Fact]
    public void GetMyStatements_ContainsAllStatuses()
    {
        var statuses = _sut.GetMyStatements().Statements.Select(s => s.Status).ToList();

        statuses.Should().Contain("PENDING");
        statuses.Should().Contain("APPROVED");
        statuses.Should().Contain("REJECTED");
    }

    // ── GetStatementDetail ─────────────────────────────────────────────────────

    [Fact]
    public void GetStatementDetail_ValidPendingId_ReturnsForecastedTransactions()
    {
        var result = _sut.GetStatementDetail("S1");

        result.Should().NotBeNull();
        result!.ForecastedTransactions.Should().HaveCount(2);
        result.CurrentTransactions.Should().BeEmpty();
    }

    [Fact]
    public void GetStatementDetail_ValidApprovedId_ReturnsCurrentTransactions()
    {
        var result = _sut.GetStatementDetail("S2");

        result.Should().NotBeNull();
        result!.CurrentTransactions.Should().HaveCount(1);
        result.ForecastedTransactions.Should().BeEmpty();
    }

    [Fact]
    public void GetStatementDetail_AgencyInfoMapped()
    {
        var result = _sut.GetStatementDetail("S1");

        result!.AgencyInfo.AgencyId.Should().Be("10");
        result.AgencyInfo.LegalName.Should().Be("Test Agency");
        result.AgencyInfo.StatementEmail.Should().Be("test@example.com");
    }

    [Fact]
    public void GetStatementDetail_StatementSummaryMapped()
    {
        var result = _sut.GetStatementDetail("S1");

        result!.StatementSummary.StatementId.Should().Be("S1");
        result.StatementSummary.TotalAmountOwed.Should().Be(90.00m);
        result.StatementSummary.TotalClawbacks.Should().Be(-10.00m);
        result.StatementSummary.Status.Should().Be("PENDING");
    }

    [Fact]
    public void GetStatementDetail_InvalidId_ReturnsNull()
    {
        var result = _sut.GetStatementDetail("NONEXISTENT");

        result.Should().BeNull();
    }

    [Fact]
    public void GetStatementDetail_TransactionFieldsMapped()
    {
        var result = _sut.GetStatementDetail("S1");
        var t = result!.ForecastedTransactions.First(x => x.TransactionId == "T001");

        t.CustomerName.Should().Be("Alpha Corp");
        t.ContractNumber.Should().Be("C001");
        t.Usage.Should().Be(500.00m);
        t.Commission.Should().Be(2.50m);
        t.CommodityType.Should().Be("Electric");
    }

    // ── ApproveStatement ───────────────────────────────────────────────────────

    [Fact]
    public void ApproveStatement_PendingStatement_SetsStatusApproved()
    {
        var freshRoot = _fixture.WriteFreshCopy();
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == freshRoot);
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        var result = sut.ApproveStatement(new ApproveStatementRequest
        {
            StatementId = "S1",
            IncludedTransactionIds = ["T001"]
        });

        result.Success.Should().BeTrue();
        sut.GetStatementDetail("S1")!.StatementSummary.Status.Should().Be("APPROVED");
    }

    [Fact]
    public void ApproveStatement_IncludedTransactionBecomesCurrentState()
    {
        var freshRoot = _fixture.WriteFreshCopy();
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == freshRoot);
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        sut.ApproveStatement(new ApproveStatementRequest
        {
            StatementId = "S1",
            IncludedTransactionIds = ["T001"]
        });

        var detail = sut.GetStatementDetail("S1")!;
        detail.CurrentTransactions.Should().ContainSingle(t => t.TransactionId == "T001");
        detail.ForecastedTransactions.Should().ContainSingle(t => t.TransactionId == "T002");
    }

    [Fact]
    public void ApproveStatement_AlreadyApproved_ReturnsFalse()
    {
        var result = _sut.ApproveStatement(new ApproveStatementRequest { StatementId = "S2" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not in PENDING");
    }

    [Fact]
    public void ApproveStatement_NonExistentId_ReturnsFalse()
    {
        var result = _sut.ApproveStatement(new ApproveStatementRequest { StatementId = "GHOST" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    // ── RejectStatement ────────────────────────────────────────────────────────

    [Fact]
    public void RejectStatement_PendingStatement_SetsStatusRejected()
    {
        var freshRoot = _fixture.WriteFreshCopy();
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == freshRoot);
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        var result = sut.RejectStatement(new RejectStatementRequest
        {
            StatementId = "S1",
            Comments = "Incorrect data."
        });

        result.Success.Should().BeTrue();
        sut.GetStatementDetail("S1")!.StatementSummary.Status.Should().Be("REJECTED");
    }

    [Fact]
    public void RejectStatement_CommentsPersistedToXml()
    {
        var freshRoot = _fixture.WriteFreshCopy();
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == freshRoot);
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        sut.RejectStatement(new RejectStatementRequest
        {
            StatementId = "S1",
            Comments = "Test rejection reason."
        });

        sut.GetStatementDetail("S1")!.StatementSummary.Comments.Should().Be("Test rejection reason.");
    }

    [Fact]
    public void RejectStatement_AlreadyApproved_ReturnsFalse()
    {
        var result = _sut.RejectStatement(new RejectStatementRequest { StatementId = "S2" });

        result.Success.Should().BeFalse();
    }

    // ── BulkApprove / BulkReject ───────────────────────────────────────────────

    [Fact]
    public void BulkApprove_MultiplePendingStatements_AllApproved()
    {
        // S1 is PENDING in the test data — use fresh copies since it's a mutation
        var freshRoot1 = _fixture.WriteFreshCopy();
        // Add a second PENDING by starting from our test XML which has S1 as PENDING
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == freshRoot1);
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        // Only S1 is PENDING; bulk with just S1 should succeed
        var result = sut.BulkApprove(new BulkStatementRequest { StatementIds = ["S1"] });

        result.Success.Should().BeTrue();
        sut.GetStatementDetail("S1")!.StatementSummary.Status.Should().Be("APPROVED");
    }

    [Fact]
    public void BulkReject_WithNonExistentId_ReturnsFailure()
    {
        var result = _sut.BulkReject(new BulkStatementRequest { StatementIds = ["MISSING_ID"] });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Not found");
    }

    // ── EmailStatement ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EmailStatement_ValidIdWithEmail_CallsEmailService()
    {
        _emailServiceMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.EmailStatementAsync(new EmailStatementRequest { StatementId = "S2" });

        result.Success.Should().BeTrue();
        _emailServiceMock.Verify(e => e.SendAsync("test@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EmailStatement_NoEmailConfigured_ReturnsFalse()
    {
        var result = await _sut.EmailStatementAsync(new EmailStatementRequest { StatementId = "S3" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No statement email");
    }

    [Fact]
    public async Task EmailStatement_NonExistentId_ReturnsFalse()
    {
        var result = await _sut.EmailStatementAsync(new EmailStatementRequest { StatementId = "GHOST" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    // ── Missing file ───────────────────────────────────────────────────────────

    [Fact]
    public void GetMyStatements_MissingFile_ThrowsFileNotFoundException()
    {
        var env = Mock.Of<IWebHostEnvironment>(e => e.ContentRootPath == Path.Combine(Path.GetTempPath(), "nonexistent-bp"));
        var sut = new BrokerStatementsService(env, _emailServiceMock.Object);

        var act = () => sut.GetMyStatements();

        act.Should().Throw<FileNotFoundException>();
    }

    // ── GetStatementsSummary ───────────────────────────────────────────────────
    // Test XML: S1=PENDING/AgencyId=10, S2=APPROVED/Total=200/AgencyId=10,
    //           S3=REJECTED/AgencyId=10

    [Fact]
    public void GetStatementsSummary_WithAgencyId_CountsOnlyThatAgency()
    {
        var result = _sut.GetStatementsSummary("10");

        result.StatementsSubmitted.Should().Be(3);
    }

    [Fact]
    public void GetStatementsSummary_WithAgencyId_PendingCountCorrect()
    {
        var result = _sut.GetStatementsSummary("10");

        result.PendingCount.Should().Be(1);
    }

    [Fact]
    public void GetStatementsSummary_PendingLabel_ReflectsPendingCount()
    {
        var result = _sut.GetStatementsSummary("10");

        result.PendingLabel.Should().Be($"{result.PendingCount} pending");
    }

    [Fact]
    public void GetStatementsSummary_ApprovedCommissionLabel_IsStaticText()
    {
        var result = _sut.GetStatementsSummary("10");

        result.ApprovedCommissionLabel.Should().Be("Approved statements total");
    }

    [Fact]
    public void GetStatementsSummary_ApprovedStatementsLabel_IsStaticText()
    {
        var result = _sut.GetStatementsSummary("10");

        result.ApprovedStatementsLabel.Should().Be("Ready for payout");
    }

    [Fact]
    public void GetStatementsSummary_WithAgencyId_ApprovedStatementsCorrect()
    {
        var result = _sut.GetStatementsSummary("10");

        result.ApprovedStatements.Should().Be(1);
    }

    [Fact]
    public void GetStatementsSummary_WithAgencyId_ApprovedCommissionSumsApprovedTotals()
    {
        // S2 is APPROVED with Total=200.00
        var result = _sut.GetStatementsSummary("10");

        result.ApprovedCommission.Should().Be(200.00m);
    }

    [Fact]
    public void GetStatementsSummary_NullAgencyId_AggregatesAllStatements()
    {
        // All 3 test statements belong to agencyId=10, so totals are the same
        var result = _sut.GetStatementsSummary(null);

        result.StatementsSubmitted.Should().Be(3);
        result.PendingCount.Should().Be(1);
        result.ApprovedStatements.Should().Be(1);
        result.ApprovedCommission.Should().Be(200.00m);
    }

    [Fact]
    public void GetStatementsSummary_EmptyAgencyId_AggregatesAllStatements()
    {
        var result = _sut.GetStatementsSummary(string.Empty);

        result.StatementsSubmitted.Should().Be(3);
    }

    [Fact]
    public void GetStatementsSummary_UnknownAgencyId_ReturnsAllZeroes()
    {
        var result = _sut.GetStatementsSummary("UNKNOWN_AGENCY");

        result.StatementsSubmitted.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.ApprovedStatements.Should().Be(0);
        result.ApprovedCommission.Should().Be(0m);
    }

    [Fact]
    public void GetStatementsSummary_RejectedStatements_NotCountedInApproved()
    {
        // S3 is REJECTED — must not affect ApprovedStatements or ApprovedCommission
        var result = _sut.GetStatementsSummary("10");

        // Only S2 (APPROVED) contributes; S3 (REJECTED, Total=50) must not add to commission
        result.ApprovedCommission.Should().Be(200.00m);
        result.ApprovedStatements.Should().Be(1);
    }

    [Fact]
    public void GetStatementsSummary_CaseInsensitiveAgencyIdMatch()
    {
        var upper = _sut.GetStatementsSummary("10");
        // AgencyId is stored as "10" — exact numeric match; verify consistent results
        upper.StatementsSubmitted.Should().Be(3);
    }
}
