using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    public const string CASE_INSENSITIVE_COLLATION = "case_insensitive";

    [Obsolete]
    static ApplicationContext()
    {
        // GlobalTypeMapper is obsolete but the new way (DataSource) is a pain to work with
        NpgsqlConnection.GlobalTypeMapper.MapEnum<UserRole>();
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) { }

    public DbSet<AccountRecovery> AccountRecoveries { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<UserConfirmation> UserConfirmations { get; set; } = null!;
    public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    public DbSet<Run> Runs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Migrates the database and reloads Npgsql types
    /// </summary>
    public void MigrateDatabase()
    {
        Database.Migrate();

        // when new extensions have been enabled by migrations, Npgsql's type cache must be refreshed
        Database.OpenConnection();
        ((NpgsqlConnection)Database.GetDbConnection()).ReloadTypes();
        Database.CloseConnection();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.HasCollation(CASE_INSENSITIVE_COLLATION, "und-u-ks-level2", "icu", deterministic: false);
        modelBuilder.HasPostgresEnum<UserRole>();
    }
}
