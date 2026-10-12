using BP.Models;
using BP.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.Data;
using System.Xml.Linq;

public class UserAccountRegistrationTests : IDisposable
{
    private readonly string _contentRootPath;
    private readonly string _xmlFilePath;
    private readonly IWebHostEnvironment _environment;

    public UserAccountRegistrationTests()
    {
        _contentRootPath = Path.Combine(Path.GetTempPath(), $"bp-account-registration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_contentRootPath, "Data"));
        _xmlFilePath = Path.Combine(_contentRootPath, "Data", "users.xml");
        new XDocument(new XElement("Users",
            new XElement("User",
                new XElement("UserId", 1),
                new XElement("EmailId", "existing@example.com"))))
            .Save(_xmlFilePath);
        _environment = Mock.Of<IWebHostEnvironment>(environment => environment.ContentRootPath == _contentRootPath);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("12345678")]
    [InlineData("001234")]
    [InlineData("00123456")]
    public void BillingValidation_ValidAccountId_HasNoErrors(string accountId)
    {
        CreateRequest(accountId).GetValidationErrors().Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("12345a")]
    [InlineData("123.45")]
    [InlineData("-12345")]
    [InlineData(" 123456")]
    [InlineData("123456 ")]
    [InlineData("123456\n")]
    public void BillingValidation_InvalidAccountId_ReturnsAccountIdError(string accountId)
    {
        CreateRequest(accountId).GetValidationErrors().Should().Contain(error => error.Contains("Account ID"));
    }

    [Theory]
    [InlineData("001234")]
    [InlineData("00123456")]
    public void CreateUser_ValidAccountId_PersistsExactValue(string accountId)
    {
        var service = new UserService(_environment);

        var user = service.CreateUser(CreateRequest(accountId));

        user.Should().NotBeNull();
        var savedUser = XDocument.Load(_xmlFilePath).Root!.Elements("User")
            .Single(element => (string?)element.Element("EmailId") == "new@example.com");
        ((string?)savedUser.Element("AccountId")).Should().Be(accountId);
    }

    [Theory]
    [InlineData("001234")]
    [InlineData("00123456")]
    public void CreateUser_DuplicateAccountIdAfterServiceReload_RejectsWithoutSaving(string accountId)
    {
        new UserService(_environment).CreateUser(CreateRequest(accountId));
        var reloadedService = new UserService(_environment);

        Action createDuplicate = () => reloadedService.CreateUser(CreateRequest(accountId, "another@example.com"));

        createDuplicate.Should().Throw<DuplicateNameException>()
            .WithMessage("Account already exists with this Account ID.");
        XDocument.Load(_xmlFilePath).Root!.Elements("User").Should().HaveCount(2);
    }

    [Fact]
    public void CreateUser_DifferentAccountIds_SavesBothAccounts()
    {
        var service = new UserService(_environment);

        service.CreateUser(CreateRequest("123456")).Should().NotBeNull();
        service.CreateUser(CreateRequest("12345678", "another@example.com")).Should().NotBeNull();

        XDocument.Load(_xmlFilePath).Root!.Elements("User").Should().HaveCount(3);
    }

    [Fact]
    public void CreateUser_DuplicateEmail_StillRejectsWithoutSaving()
    {
        var service = new UserService(_environment);

        service.CreateUser(CreateRequest("123456", "existing@example.com")).Should().BeNull();

        XDocument.Load(_xmlFilePath).Root!.Elements("User").Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateUser_ConcurrentDuplicateAccountIds_SavesOnlyOneAccount()
    {
        var attempts = Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            try
            {
                return new UserService(_environment)
                    .CreateUser(CreateRequest("123456", $"user{index}@example.com")) is not null;
            }
            catch (DuplicateNameException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(attempts);

        results.Count(created => created).Should().Be(1);
        XDocument.Load(_xmlFilePath).Root!.Elements("User").Should().HaveCount(2);
    }

    [Fact]
    public void BrokerRegistration_WithoutAccountId_RemainsValid()
    {
        var request = CreateRequest(string.Empty);
        request.IsBroker = true;
        request.OrganizationName = "Test Brokerage";
        request.Address1 = string.Empty;
        request.City = string.Empty;
        request.State = string.Empty;
        request.Zipcode = string.Empty;
        request.Country = string.Empty;

        request.GetValidationErrors().Should().BeEmpty();
        new UserService(_environment).CreateUser(request).Should().NotBeNull();
        XDocument.Load(_xmlFilePath).Root!.Elements("User")
            .Should().NotContain(element => element.Element("AccountId") != null);
    }

    private static CreateAccountRequest CreateRequest(string accountId, string emailId = "new@example.com")
    {
        return new CreateAccountRequest
        {
            AccountId = accountId,
            Name = "Test User",
            EmailId = emailId,
            Password = "TestPassword1!",
            ContactNumber = "5551234567",
            PrimaryBusinessNumber = "5559876543",
            Address1 = "123 Test Street",
            City = "Houston",
            State = "TX",
            Zipcode = "77001",
            Country = "US"
        };
    }

    public void Dispose()
    {
        Directory.Delete(_contentRootPath, true);
    }
}