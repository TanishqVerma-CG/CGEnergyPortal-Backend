namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with test XML files.
/// Shared across all tests in a test class via IClassFixture.
/// </summary>
public sealed class BrokerHomeServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-tests-{Guid.NewGuid():N}");

    public BrokerHomeServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);

        File.WriteAllText(Path.Combine(dataPath, "broker-home-metrics.xml"), TestXmlData.HomeMetrics);
        File.WriteAllText(Path.Combine(dataPath, "broker-renewal-watchlist.xml"), TestXmlData.RenewalWatchlist);
        File.WriteAllText(Path.Combine(dataPath, "broker-open-support-requests.xml"), TestXmlData.OpenSupportRequests);
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
