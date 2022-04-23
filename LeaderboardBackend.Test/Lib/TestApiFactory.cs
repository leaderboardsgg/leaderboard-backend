using BCryptNet = BCrypt.Net.BCrypt;
using LeaderboardBackend.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace LeaderboardBackend.Test.Lib;

internal class TestApiFactory : WebApplicationFactory<Program>
{
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

	public TestApiClient CreateTestApiClient()
	{
		HttpClient client = this.CreateClient();
		return new TestApiClient(client);
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
			Id = TestInitCommonFields.Admin.Id,
			Username = TestInitCommonFields.Admin.Username,
			Email = TestInitCommonFields.Admin.Email,
			Password = BCryptNet.EnhancedHashPassword(TestInitCommonFields.Admin.Password),
			Admin = true,
		};

		dbContext.Add<User>(admin);
		dbContext.Add<Leaderboard>(leaderboard);

		dbContext.SaveChanges();
	}
}
