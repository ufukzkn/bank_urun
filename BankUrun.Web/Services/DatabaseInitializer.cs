using System.Data;
using BankUrun.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class DatabaseInitializer(
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    private const string SeedMarker = "mock-v18";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("DatabaseInitialization:Migrate"))
        {
            return;
        }

        logger.LogInformation("Veritabanı migration'ları uygulanıyor.");
        await db.Database.MigrateAsync(cancellationToken);

        if (!configuration.GetValue<bool>("DatabaseInitialization:Seed"))
        {
            return;
        }

        var seedExists = await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(log => log.Action == "SeedMockData" && log.EntityKey == SeedMarker, cancellationToken);
        if (seedExists)
        {
            logger.LogInformation("Mock veri işareti bulundu; seed atlandı.");
            return;
        }

        var configuredPath = configuration["DatabaseInitialization:SeedScriptPath"];
        var seedPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "scripts", "seed-mock-data.sql")
            : configuredPath;
        if (!File.Exists(seedPath))
        {
            throw new FileNotFoundException("Mock veri script'i bulunamadı.", seedPath);
        }

        logger.LogInformation("İlk kurulum mock verisi yükleniyor.");
        var sql = await File.ReadAllTextAsync(seedPath, cancellationToken);
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 180;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
