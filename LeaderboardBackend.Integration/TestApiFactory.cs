using LeaderboardBackend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeaderboardBackend.Integration;

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
			dbContext.Database.Migrate();
		});
	}
}
