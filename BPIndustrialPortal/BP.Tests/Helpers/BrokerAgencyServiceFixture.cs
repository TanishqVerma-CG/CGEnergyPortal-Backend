namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with the broker-commission-agencies.xml test file.
/// Shared across all tests in a test class via IClassFixture.
/// </summary>
public sealed class BrokerAgencyServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-tests-agency-{Guid.NewGuid():N}");

    public BrokerAgencyServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "broker-commission-agencies.xml"), TestXmlData.CommissionAgencies);
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
