using System;
using System.Threading.Tasks;
using LeaderboardBackend.Test.Fixtures;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

// Fixtures apply to all tests in its namespace
// It has no namespace on purpose, so that the fixture applies to all tests in this assembly

[SetUpFixture] // https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html
internal class PostgresDatabaseFixture
{
    public static PostgreSqlContainer? PostgresContainer { get; private set; }
    public static string? Database { get; private set; }
    public static string? Username { get; private set; }
    public static string? Password { get; private set; }
    public static int Port { get; private set; }
    public static bool HasCreatedTemplate { get; private set; } = false;
    private static string TemplateDatabase => Database! + "_template";

    [OneTimeSetUp]
    public static async Task OneTimeSetup()
    {
        if (TestConfig.DatabaseBackend != DatabaseBackend.TestContainer)
        {
            return;
        }

        PostgresContainer = new PostgreSqlBuilder()
            .WithTmpfsMount("/var/lib/postgresql/data") // db files in-memory
            .Build();
        await PostgresContainer.StartAsync();

        NpgsqlConnectionStringBuilder connStrBuilder = new(PostgresContainer.GetConnectionString());
        Username = connStrBuilder.Username!;
        Password = connStrBuilder.Password!;
        Database = connStrBuilder.Database!;
        Port = connStrBuilder.Port;
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        if (PostgresContainer is null)
        {
            return;
        }

        await PostgresContainer.DisposeAsync();
    }

    public static void CreateTemplateFromCurrentDb()
    {
        ThrowIfNotInitialized();

        NpgsqlConnection.ClearAllPools(); // can't drop a DB if connections remain open
        using NpgsqlDataSource conn = CreateConnectionToTemplate();
        conn.CreateCommand(
                @$"
			DROP DATABASE IF EXISTS {TemplateDatabase};
			CREATE DATABASE {TemplateDatabase}
				WITH TEMPLATE {Database}
				OWNER '{Username}';
			"
            )
            .ExecuteNonQuery();
        HasCreatedTemplate = true;
    }

    // It is faster to recreate the db from an already seeded template
    // compared to dropping the db and recreating it from scratch
    public static void ResetDatabaseToTemplate()
    {
        ThrowIfNotInitialized();
        if (!HasCreatedTemplate)
        {
            throw new InvalidOperationException("Database template has not been created.");
        }
        NpgsqlConnection.ClearAllPools(); // can't drop a DB if connections remain open
        using NpgsqlDataSource conn = CreateConnectionToTemplate();
        conn.CreateCommand(
                @$"
			DROP DATABASE IF EXISTS {Database};
			CREATE DATABASE {Database}
				WITH TEMPLATE {TemplateDatabase}
				OWNER '{Username}';
			"
            )
            .ExecuteNonQuery();
    }

    private static NpgsqlDataSource CreateConnectionToTemplate()
    {
        ThrowIfNotInitialized();
        NpgsqlConnectionStringBuilder connStrBuilder =
            new(PostgresContainer!.GetConnectionString()) { Database = "template1" };
        return NpgsqlDataSource.Create(connStrBuilder);
    }

    private static void ThrowIfNotInitialized()
    {
        if (PostgresContainer is null)
        {
            throw new InvalidOperationException("Postgres container is not initialized.");
        }
    }
}
