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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseSeeding((context, _) =>
            {
                Console.WriteLine("Creating users");
                User? admin = context.Set<User>().FirstOrDefault(u => u.Email == "admin@leaderboards.gg");
                if (admin == null)
                {
                    admin = new()
                    {
                        Email = "admin@leaderboards.gg",
                        Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                        Username = "admin",
                        Role = UserRole.Administrator,
                    };
                    context.Add(admin);
                }

                User? user = context.Set<User>().FirstOrDefault(u => u.Email == "user1@leaderboards.gg");
                if (user == null)
                {
                    user = new()
                    {
                        Email = "user1@leaderboards.gg",
                        Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                        Username = "user1",
                        Role = UserRole.Confirmed,
                    };
                    context.Add(user);
                }

                Console.WriteLine("Creating leaderboard");
                Leaderboard? board = context.Set<Leaderboard>().FirstOrDefault(b => b.Slug == "mario-64");
                if (board == null)
                {
                    board = new()
                    {
                        Name = "Mario 64",
                        Slug = "mario-64",
                        Info = "Jump Man wahoos in 3D for the first time.",
                    };
                    context.Add(board);
                }

                Console.WriteLine("Creating category");
                Category? cat = context.Set<Category>().FirstOrDefault(c => c.Slug == "category");
                if (cat == null)
                {
                    cat = new()
                    {
                        Name = "category",
                        Slug = "category",
                        LeaderboardId = board!.Id,
                        SortDirection = SortDirection.Ascending,
                        Type = RunType.Time,
                    };
                    context.Add(cat);
                }

                Console.WriteLine("Creating runs");
                foreach (Run run in context.Set<Run>().Where(r => r.Category == cat))
                {
                    context.Remove(run);
                }

                Run[] runs = [
                    new()
                    {
                        Category = cat!,
                        Info = "Run attempt description.",
                        User = admin!,
                        TimeOrScore = 1000L,
                    },
                    new()
                    {
                        Category = cat!,
                        Info = "Run attempt description.",
                        User = user!,
                        TimeOrScore = 1100L,
                    },
                ];

                context.AddRange(runs);
                context.SaveChanges();
            }
        ).UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            Console.WriteLine("Creating users");
            User? admin = await context.Set<User>().FirstOrDefaultAsync(u => u.Email == "admin@leaderboards.gg", cancellationToken);
            if (admin == null)
            {
                await context.AddRangeAsync(new User()
                {
                    Email = "admin@leaderboards.gg",
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                    Username = "admin",
                    Role = UserRole.Administrator,
                }, cancellationToken);
            }

            User? user = await context.Set<User>().FirstOrDefaultAsync(u => u.Email == "user1@leaderboards.gg", cancellationToken);
            if (user == null)
            {
                await context.AddRangeAsync(new User()
                {
                    Email = "user1@leaderboards.gg",
                    Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                    Username = "user1",
                    Role = UserRole.Confirmed,
                }, cancellationToken);
            }

            Console.WriteLine("Creating leaderboard");
            Leaderboard? board = await context.Set<Leaderboard>().FirstOrDefaultAsync(b => b.Slug == "mario-64", cancellationToken);
            if (board == null)
            {
                await context.AddRangeAsync(new Leaderboard()
                {
                    Name = "Mario 64",
                    Slug = "mario-64",
                    Info = "Jump Man wahoos in 3D for the first time."
                }, cancellationToken);
            }

            Console.WriteLine("Creating category");
            Category? cat = await context.Set<Category>().FirstOrDefaultAsync(c => c.Slug == "category", cancellationToken);
            if (cat == null)
            {
                await context.AddRangeAsync(new Category()
                {
                    Name = "category",
                    Slug = "category",
                    LeaderboardId = board!.Id,
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Time,
                }, cancellationToken);
            }

            Console.WriteLine("Creating runs");
            foreach (Run run in context.Set<Run>().Where(r => r.Category == cat))
            {
                context.Remove(run);
            }

            Run[] runs = [
                new()
                {
                    Category = cat!,
                    Info = "Run attempt description.",
                    User = admin!,
                    TimeOrScore = 1000L,
                },
                new()
                {
                    Category = cat!,
                    Info = "Run attempt description.",
                    User = user!,
                    TimeOrScore = 1100L,
                },
            ];

            await context.AddRangeAsync(runs, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        });
}
