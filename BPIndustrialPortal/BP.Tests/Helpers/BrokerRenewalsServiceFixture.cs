namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with the broker-renewals.xml test data.
/// Shared across all tests in a class via IClassFixture.
/// </summary>
public sealed class BrokerRenewalsServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-renewals-tests-{Guid.NewGuid():N}");

    public BrokerRenewalsServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "broker-renewals.xml"), TestXmlData.BrokerRenewals);
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
