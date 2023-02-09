using System.Net.Http;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.TestApi;

internal class TestApiFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Set the environment for the run to Staging
		builder.UseEnvironment(Environments.Staging);

		base.ConfigureWebHost(builder);

		builder.ConfigureServices(services =>
		{
			int testPort = DotNetEnv.Env.GetInt("POSTGRES_TEST_PORT", -1);
			if (testPort >= 0)
			{
				services.Configure<ApplicationContextConfig>(conf =>
				{
					if (conf.Pg is not null)
					{
						conf.Pg.Port = (ushort)testPort;
					}
				});
			}

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
		HttpClient client = CreateClient();
		return new TestApiClient(client);
	}

	private static void Seed(ApplicationContext dbContext)
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

		dbContext.Add(admin);
		dbContext.Add(leaderboard);

		dbContext.SaveChanges();
	}
}
