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
	private static User s_admin = new()
	{
		Id = System.Guid.NewGuid(),
		Username = "AyyLmaoGaming",
		Email = "ayylmaogaming@alg.gg",
		Password = "P4ssword",
		Admin = true,
	};

	public User GetAdmin() => s_admin;

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

		User admin = new()
		{
			Id = s_admin.Id,
			Username = s_admin.Username,
			Email = s_admin.Email,
			Password = BCryptNet.EnhancedHashPassword(s_admin.Password),
			Admin = true,
		};

		dbContext.Add<User>(admin);
		dbContext.Add<Leaderboard>(leaderboard);

		dbContext.SaveChanges();
	}
}
