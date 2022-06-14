using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeaderboardBackend.Models.Entities;

public class ApplicationContext : DbContext
{
	static ApplicationContext()
		=> NpgsqlConnection.GlobalTypeMapper.MapEnum<MetricType>()
			.MapEnum<RunStatus>();

	public ApplicationContext(DbContextOptions<ApplicationContext> options)
		: base(options) { }

	public DbSet<Ban> Bans { get; set; } = null!;
	public DbSet<Category> Categories { get; set; } = null!;
	public DbSet<Judgement> Judgements { get; set; } = null!;
	public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
	public DbSet<Metric> Metrics { get; set; } = null!;
	public DbSet<Modship> Modships { get; set; } = null!;
	public DbSet<Run> Runs { get; set; } = null!;
	public DbSet<Participation> Participations { get; set; } = null!;
	public DbSet<User> Users { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Judgement>()
			.Property(j => j.CreatedAt)
			.HasDefaultValueSql("now()");

		modelBuilder.Entity<Ban>()
			.Property(b => b.CreatedAt)
			.HasDefaultValueSql("now()");

		modelBuilder.HasPostgresEnum<MetricType>()
					.HasPostgresEnum<RunStatus>();
	}
}
