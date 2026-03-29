using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    private readonly IClock _clock;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
