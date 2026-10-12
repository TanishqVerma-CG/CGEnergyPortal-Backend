using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BP.Tests.Helpers;

/// <summary>
/// WebApplicationFactory that points the content root at the real BP project directory
/// so all XML data files and appsettings.json are found at runtime.
/// Sets environment to "Testing" so HTTPS redirect is suppressed.
/// </summary>
public sealed class BrokerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(FindBpProjectPath());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtTokenHelper.Key
            }));
        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter("Hangfire", LogLevel.None);
        });
    }

    /// <summary>
    /// Walks up the directory tree from the test binary until it finds the
    /// folder containing the solution file, then returns the sibling BP/ folder.
    /// </summary>
    private static string FindBpProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length > 0)
            {
                var bpPath = Path.Combine(dir.FullName, "BP");
                if (Directory.Exists(bpPath))
                    return bpPath;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the BP project directory. Ensure the solution (.sln) file " +
            "is an ancestor of the test binary output directory.");
    }
}
