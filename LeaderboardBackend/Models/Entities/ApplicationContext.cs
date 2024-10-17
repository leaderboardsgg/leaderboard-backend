using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NodaTime;
using Npgsql;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    private readonly IClock _clock;

    [Obsolete]
    static ApplicationContext()
    {
        // GlobalTypeMapper is obsolete but the new way (DataSource) is a pain to work with
        NpgsqlConnection.GlobalTypeMapper.MapEnum<UserRole>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<SortDirection>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<RunType>();
    }

    private void AddCreationTimestamp(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.State is EntityState.Added && e.Entry.Entity is IHasCreationTimestamp entity)
        {
            entity.CreatedAt = _clock.GetCurrentInstant();
        }
    }

    private void SetUpdateTimestamp(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.State is EntityState.Modified && e.Entry.Entity is IHasUpdateTimestamp entity)
        {
            entity.UpdatedAt = _clock.GetCurrentInstant();
        }
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options, IClock clock)
        : base(options)
    {
        _clock = clock;
        ChangeTracker.Tracked += AddCreationTimestamp;
        ChangeTracker.StateChanged += SetUpdateTimestamp;
    }

    public DbSet<AccountRecovery> AccountRecoveries { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<AccountConfirmation> AccountConfirmations { get; set; } = null!;
    public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    public DbSet<Run> Runs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Migrates the database and reloads Npgsql types
    /// </summary>
    public void MigrateDatabase()
    {
        Database.Migrate();
        bool tempConnection = false;
        NpgsqlConnection connection = (NpgsqlConnection)Database.GetDbConnection();

        if (connection.State is ConnectionState.Closed)
        {
            tempConnection = true;
            Database.OpenConnection();
        }

        // when new extensions have been enabled by migrations, Npgsql's type cache must be refreshed
        connection.ReloadTypes();

        if (tempConnection)
        {
            Database.CloseConnection();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasPostgresEnum<UserRole>();
        modelBuilder.HasPostgresEnum<SortDirection>();
        modelBuilder.HasPostgresEnum<RunType>();
    }
}
