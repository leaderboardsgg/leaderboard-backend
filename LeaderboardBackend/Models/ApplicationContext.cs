using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Models;

public class ApplicationContext : DbContext
{
	public ApplicationContext(DbContextOptions<ApplicationContext> options)
		: base(options) { }

	public DbSet<Ban> Bans { get; set; } = null!;
	public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
	public DbSet<User> Users { get; set; } = null!;
}
