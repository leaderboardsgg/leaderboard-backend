using dotenv.net;
using dotenv.net.Utilities;
using LeaderboardBackend.Jobs.Core;
using LeaderboardBackend.Jobs.Jobs;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

DotEnv.Load(options: new DotEnvOptions(
	ignoreExceptions: false,
	overwriteExistingVars: false,
	envFilePaths: new[] { "../.env" },
	trimValues: true
));

if (EnvReader.TryGetBooleanValue("USE_IN_MEMORY_DB", out bool inMemoryDb) && inMemoryDb)
{
	Console.WriteLine("Jobs do not run on in memory database.");
	return;
}

if (!EnvReader.TryGetStringValue("POSTGRES_HOST", out string host)
	|| !EnvReader.TryGetStringValue("POSTGRES_USER", out string user)
	|| !EnvReader.TryGetStringValue("POSTGRES_PASSWORD", out string password)
	|| !EnvReader.TryGetStringValue("POSTGRES_DB", out string db)
	|| !EnvReader.TryGetIntValue("POSTGRES_PORT", out int port))
{
	Console.WriteLine("Missing a required database environment variable.");
	return;
}

string connectionString =
	$"Server={host};Port={port};User Id={user};Password={password};" +
	$"Database={db};Include Error Detail=true";

ApplicationContext context = new(
	new DbContextOptionsBuilder<ApplicationContext>()
		.UseNpgsql(connectionString)
		.UseSnakeCaseNamingConvention()
		.Options
);

List<IJob> jobs = new()
{
	new ScaffoldUser(context),
	new ScaffoldLeaderboardAndMod(context),
};

IJob? chosen = null;

if (args.Length > 0)
{
	chosen = jobs.Where(j => j.CommandName == args[0]).SingleOrDefault();
}
else
{
	ConsoleMenu<IJob> menu = new("Choose a job", jobs);
	chosen = menu.Choose();
}

Console.WriteLine();

if (chosen is null)
{
	Console.WriteLine("See ya!");

	return;
}

try
{
	await chosen.Run();
}
catch (Exception ex)
{
	Console.WriteLine($"Job {chosen.CommandName} failed: {ex.Message}");
}
