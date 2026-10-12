namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with the three pricing XML test files
/// required by ContractService pricing methods.
/// Shared across all tests in a class via IClassFixture.
/// </summary>
public sealed class ContractPricingServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-pricing-tests-{Guid.NewGuid():N}");

    public ContractPricingServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);

        File.WriteAllText(Path.Combine(dataPath, "pricing-intelligence.xml"),   TestXmlData.PricingIntelligence);
        File.WriteAllText(Path.Combine(dataPath, "pricing-trend.xml"),          TestXmlData.PricingTrend);
        File.WriteAllText(Path.Combine(dataPath, "pricing-snapshot-details.xml"), TestXmlData.PricingSnapshotDetails);
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
