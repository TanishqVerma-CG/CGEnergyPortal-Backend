namespace BP.Tests.Helpers;

/// <summary>
/// Creates a temporary directory pre-populated with the broker-commission-statements.xml test file.
/// Shared across all tests in a test class via IClassFixture.
/// Note: tests that mutate the XML (Approve/Reject) copy this to a fresh temp dir per test via WriteFreshCopy().
/// </summary>
public sealed class BrokerStatementsServiceFixture : IDisposable
{
    public string ContentRootPath { get; } =
        Path.Combine(Path.GetTempPath(), $"bp-tests-statements-{Guid.NewGuid():N}");

    public BrokerStatementsServiceFixture()
    {
        var dataPath = Path.Combine(ContentRootPath, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "broker-commission-statements.xml"), TestXmlData.CommissionStatements);
    }

    /// <summary>
    /// Returns a fresh isolated content root with a copy of the statements XML.
    /// Use in mutation tests to avoid cross-test state pollution.
    /// </summary>
    public string WriteFreshCopy()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"bp-tests-stmt-mut-{Guid.NewGuid():N}");
        var dataPath = Path.Combine(tempRoot, "Data");
        Directory.CreateDirectory(dataPath);
        File.WriteAllText(Path.Combine(dataPath, "broker-commission-statements.xml"), TestXmlData.CommissionStatements);
        return tempRoot;
    }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
            Directory.Delete(ContentRootPath, recursive: true);
    }
}
