using System.Threading.Tasks;
using Npgsql;
using NUnit.Framework;
using Testcontainers.PostgreSql;

// Fixtures apply to all tests in its namespace
// It has no namespace on purpose, so that the fixture applies to all tests in this assembly

[SetUpFixture] // https://docs.nunit.org/articles/nunit/writing-tests/attributes/setupfixture.html
public class PostgresDatabaseFixture
{
    public static PostgreSqlContainer? PostgresContainer { get; private set; }
    public static string? Database { get; private set; }
    public static string? Username { get; private set; }
    public static string? Password { get; private set; }
    public static int Port { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetup()
    {
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
}
