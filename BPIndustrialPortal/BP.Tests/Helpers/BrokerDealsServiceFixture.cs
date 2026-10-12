namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with the broker-commission-deals.xml test file.
/// Shared across all tests in a test class via IClassFixture.
/// </summary>
public sealed class BrokerDealsServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-tests-deals-{Guid.NewGuid():N}");

    public BrokerDealsServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "broker-commission-deals.xml"), TestXmlData.CommissionDeals);
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
