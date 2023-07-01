using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    public const string CASE_INSENSITIVE_COLLATION = "case_insensitive";

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) { }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    public DbSet<Run> Runs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.HasCollation(CASE_INSENSITIVE_COLLATION, "und-u-ks-level2", "icu", deterministic: false);
    }
}
