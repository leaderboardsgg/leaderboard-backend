using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Test.Helpers;

internal class ApplicationContextFactory
{
	public static ApplicationContext CreateNewContext()
	{
		DbContextOptionsBuilder<ApplicationContext> optionsBuilder = new();
		optionsBuilder.UseInMemoryDatabase("LeaderboardUnitTests");
		ApplicationContext context = new(optionsBuilder.Options);
		context.Database.EnsureDeleted();
		context.Database.EnsureCreated();
		return context;
	}
}
