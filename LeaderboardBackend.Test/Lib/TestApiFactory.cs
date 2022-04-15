using BCryptNet = BCrypt.Net.BCrypt;
using LeaderboardBackend.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeaderboardBackend.Test.Lib;

internal class TestApiFactory : WebApplicationFactory<Program>
{
	// Value needs to be set in `Seed()`, as the constructor doesn't actually get called
	// in ConfigureWebhost.
	// Seems like a smell, I know.
	private static User Admin = null!;
	private static readonly string AdminPassword = "P4ssword";

	// TODO: Make this a singleton instead of creating a new object all the time
	// TODO: Also maybe only save the generated ID
	public User GetAdmin() => new()
	{
		Id = Admin.Id,
		Username = "AyyLmaoGaming",
		Email = "ayylmaogaming@alg.gg",
		Password = AdminPassword,
		Admin = true,
	};

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Set the environment for the run to Staging
		builder.UseEnvironment(Environments.Staging);

		base.ConfigureWebHost(builder);

		builder.ConfigureServices(services =>
		{
			// Reset the database on every test run
			using ServiceProvider scope = services.BuildServiceProvider();
			ApplicationContext dbContext = scope.GetRequiredService<ApplicationContext>();
			dbContext.Database.EnsureDeleted();
			if (dbContext.Database.IsInMemory())
			{
				dbContext.Database.EnsureCreated();
			}
			else
			{
				dbContext.Database.Migrate();
			}
			Seed(dbContext);
		});
	}

	private void Seed(ApplicationContext dbContext)
	{

		Leaderboard leaderboard = new()
		{
			Name = "Mario Goes to Jail",
			Slug = "mario-goes-to-jail"
		};

		Admin = new()
		{
			Username = "AyyLmaoGaming",
			Email = "ayylmaogaming@alg.gg",
			Password = BCryptNet.EnhancedHashPassword(AdminPassword),
			Admin = true,
		};

		dbContext.Add<User>(Admin);
		dbContext.Add<Leaderboard>(leaderboard);

		dbContext.SaveChanges();
	}
}
