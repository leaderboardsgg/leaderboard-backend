using System;
using System.Net.Http;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Fixtures;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.TestApi;

public class TestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the environment for the run to Staging
        builder.UseEnvironment(Environments.Staging);

        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            if (TestConfig.DatabaseBackend == DatabaseBackend.TestContainer)
            {
                if (PostgresDatabaseFixture.PostgresContainer is null)
                {
                    throw new InvalidOperationException("Postgres container is not initialized.");
                }

                services.Configure<ApplicationContextConfig>(conf =>
                {
                    conf.UseInMemoryDb = false;
                    conf.Pg = new PostgresConfig
                    {
                        Db = PostgresDatabaseFixture.Database!,
                        Port = (ushort)PostgresDatabaseFixture.Port,
                        Host = PostgresDatabaseFixture.PostgresContainer.Hostname,
                        User = PostgresDatabaseFixture.Username!,
                        Password = PostgresDatabaseFixture.Password!
                    };
                });
            }
            else
            {
                services.Configure<ApplicationContextConfig>(conf =>
                {
                    conf.UseInMemoryDb = true;
                });
            }

            using IServiceScope scope = services.BuildServiceProvider().CreateScope();
            ApplicationContext dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            switch (TestConfig.DatabaseBackend)
            {
                case DatabaseBackend.TestContainer when !PostgresDatabaseFixture.HasCreatedTemplate:
                    dbContext.Database.Migrate();
                    Seed(dbContext);
                    PostgresDatabaseFixture.CreateTemplateFromCurrentDb();
                    break;
                case DatabaseBackend.InMemory:
                    if (dbContext.Database.EnsureCreated())
                    {
                        Seed(dbContext);
                    }

                    break;
            }
        });
    }

    public TestApiClient CreateTestApiClient()
    {
        HttpClient client = CreateClient();
        return new TestApiClient(client);
    }

    private static void Seed(ApplicationContext dbContext)
    {
        Leaderboard leaderboard =
            new() { Name = "Mario Goes to Jail", Slug = "mario-goes-to-jail" };

        User admin =
            new()
            {
                Id = TestInitCommonFields.Admin.Id,
                Username = TestInitCommonFields.Admin.Username,
                Email = TestInitCommonFields.Admin.Email,
                Password = BCryptNet.EnhancedHashPassword(TestInitCommonFields.Admin.Password),
                Role = UserRole.Administrator,
            };

        dbContext.Add(admin);
        dbContext.Add(leaderboard);

        dbContext.SaveChanges();
    }

    /// <summary>
    /// Deletes and recreates the database
    /// </summary>
    public void ResetDatabase()
    {
        switch (TestConfig.DatabaseBackend)
        {
            case DatabaseBackend.InMemory:
                ResetInMemoryDb();
                break;
            case DatabaseBackend.TestContainer:
                PostgresDatabaseFixture.ResetDatabaseToTemplate();
                break;
            default:
                throw new NotImplementedException("Database reset is not implemented.");
        }
    }

    private void ResetInMemoryDb()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        Seed(dbContext);
    }
}
