using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace ECG_API.Tests;

public class EcgApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ecg-test-{Guid.NewGuid():N}.db");
    private const string TestKey = "itsomenewtestkeyforjswtunixtests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", TestKey);
        builder.UseSetting("ConnectionStrings:EcgDb", "Data Source=" + _dbPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}
