using System;

namespace LeaderboardBackend.Test.Fixtures;

internal static class TestConfig
{
    static TestConfig()
    {
        Bogus.Randomizer.Seed = new Random(43817269); // fixed seed for repeatable tests
    }
}
