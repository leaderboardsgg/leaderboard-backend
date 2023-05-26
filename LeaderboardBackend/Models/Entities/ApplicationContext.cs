using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
    public static readonly Guid s_seedAdminId = new("421bb896-1990-48c6-8b0c-d69f56d6746a");

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options) { }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    public DbSet<Run> Runs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<User>()
            .HasData(
                new User
                {
                    Id = s_seedAdminId,
                    Admin = true,
                    Email = "omega@star.com",
                    Password = "$2a$11$tNvA94WqpJ.O7S7D6lVMn.E/UxcFYztl3BkcnBj/hgE8PY/8nCRQe", // "3ntr0pyChaos"
                    Username = "Galactus"
                }
            );
    }
}
