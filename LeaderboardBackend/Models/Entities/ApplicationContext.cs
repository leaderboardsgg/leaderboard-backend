using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
	public ApplicationContext(DbContextOptions<ApplicationContext> options)
		: base(options) { }

	public DbSet<Ban> Bans { get; set; } = null!;
	public DbSet<Category> Categories { get; set; } = null!;
	public DbSet<Judgement> Judgements { get; set; } = null!;
	public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
	public DbSet<Modship> Modships { get; set; } = null!;
	public DbSet<Run> Runs { get; set; } = null!;
	public DbSet<Participation> Participations { get; set; } = null!;
	public DbSet<User> Users { get; set; } = null!;

	// TODO: Verify timestamp's generated to UTC
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Judgement>()
			.Property(j => j.CreatedAt)
			.HasDefaultValueSql("getdate()");
	}
}
