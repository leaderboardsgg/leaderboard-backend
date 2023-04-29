using System;

namespace LeaderboardBackend.Test.Fixtures;

internal static class TestConfig
{
	public static DatabaseBackend DatabaseBackend { get; private set; } = DatabaseBackend.TestContainer;

	static TestConfig()
	{
		string? backendVar = Environment.GetEnvironmentVariable("INTEGRATION_TESTS_DB_BACKEND");
		if (Enum.TryParse(backendVar, out DatabaseBackend dbBackend))
		{
			DatabaseBackend = dbBackend;
		}

        Bogus.Randomizer.Seed = new Random(43817269); // fixed seed for repeatable tests
	}
}

internal enum DatabaseBackend
{
	InMemory,
	TestContainer,
}
