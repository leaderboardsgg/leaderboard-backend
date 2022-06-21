using System.Net.Http;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
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
			// Reset the database on every test run
			using ServiceProvider scope = services.BuildServiceProvider();
			ApplicationContext dbContext = scope.GetRequiredService<ApplicationContext>();
			dbContext.Database.EnsureDeleted();
			if (dbContext.Database.IsInMemory())
			{
				dbContext.Database.EnsureCreated();
			} else
			{
				dbContext.Database.Migrate();

				// We need to tell Npgsql to reload all types after applying migrations.
				// Ref: https://www.npgsql.org/efcore/mapping/enum.html
				NpgsqlConnection conn = (NpgsqlConnection)dbContext.Database.GetDbConnection();
				conn.Open();
				conn.ReloadTypes();
				conn.Close();
			}
			Seed(dbContext);
		});
	}

	public TestApiClient CreateTestApiClient()
	{
		HttpClient client = CreateClient();
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
