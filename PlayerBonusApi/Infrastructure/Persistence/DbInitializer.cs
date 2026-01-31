using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task EnsureDatabaseAndMigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        var connectionString = config.GetConnectionString("Default")
                        ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

        var targetDbName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrWhiteSpace(targetDbName))
            throw new InvalidOperationException("Default connection string has no Database specified.");

        // Ensure database exists
        await EnsureDatabaseExistsAsync(connectionString, targetDbName, logger);

        // Apply migrations
        var db = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();

        // Seed if empty
        var any = await db.Players.IgnoreQueryFilters().AnyAsync();
        if (any)
        {
            logger.LogInformation("Seed skipped (data exists).");
            return;
        }

        logger.LogInformation("Seeding initial data...");

        var player1 = new Player { Name = "Alice Johnson", Email = "alice.johnson@example.com" };
        var player2 = new Player { Name = "Mark Petrov", Email = "mark.petrov@example.com" };

        db.Players.AddRange(player1, player2);
        await db.SaveChangesAsync();

        db.PlayerBonuses.AddRange(
            new PlayerBonus { PlayerId = player1.Id, BonusType = BonusType.Welcome, Amount = 50m, IsActive = true },
            new PlayerBonus { PlayerId = player1.Id, BonusType = BonusType.FreeSpins, Amount = 20m, IsActive = true },
            new PlayerBonus { PlayerId = player2.Id, BonusType = BonusType.Cashback, Amount = 15m, IsActive = true }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Seeding done.");
    }

    private static async Task EnsureDatabaseExistsAsync(string adminConnectionString, string dbName, ILogger logger)
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();

        await using (var existsCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name;",
            conn))
        {
            existsCmd.Parameters.AddWithValue("name", dbName);
            var exists = await existsCmd.ExecuteScalarAsync();

            if (exists is not null)
            {
                logger.LogInformation("Database '{DbName}' already exists.", dbName);
                return;
            }
        }

        logger.LogInformation("Database '{DbName}' not found. Creating...", dbName);

        // CREATE DATABASE cannot be parameterized => sanitize by allowing only safe chars
        if (!IsSafeDbName(dbName))
            throw new InvalidOperationException($"Unsafe database name: '{dbName}'");

        await using var createCmd = new NpgsqlCommand($@"CREATE DATABASE ""{dbName}"";", conn);
        await createCmd.ExecuteNonQueryAsync();

        logger.LogInformation("Database '{DbName}' created.", dbName);
    }

    private static bool IsSafeDbName(string name)
        => name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
}
