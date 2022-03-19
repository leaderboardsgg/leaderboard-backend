using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
