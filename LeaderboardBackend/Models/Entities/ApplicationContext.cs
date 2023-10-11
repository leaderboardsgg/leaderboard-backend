using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    public const string CASE_INSENSITIVE_COLLATION = "case_insensitive";
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) { }

    public DbSet<AccountRecovery> AccountRecoveries { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<AccountConfirmation> AccountConfirmations { get; set; } = null!;
    public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    public DbSet<Run> Runs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public void MigrateDatabase()
    {
        Database.Migrate();
        NpgsqlConnection connection = (NpgsqlConnection)Database.GetDbConnection();
        connection.Open();

        try
        {
            connection.ReloadTypes();
        }
        finally
        {
            connection.Close();
        }
    }

    /// <summary>
    /// Migrates the database and reloads Npgsql types
    /// </summary>
    public async Task MigrateDatabaseAsync()
    {
        await Database.MigrateAsync();

        // when new extensions have been enabled by migrations, Npgsql's type cache must be refreshed
        NpgsqlConnection connection = (NpgsqlConnection)Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            await connection.ReloadTypesAsync();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.HasCollation(CASE_INSENSITIVE_COLLATION, "und-u-ks-level2", "icu", deterministic: false);
        modelBuilder.HasPostgresEnum<UserRole>();
    }
}
