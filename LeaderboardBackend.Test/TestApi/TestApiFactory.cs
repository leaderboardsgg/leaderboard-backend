using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Npgsql;
using Respawn;
using Respawn.Graph;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.TestApi;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private static bool _migrated = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the environment for the run to Staging
        builder.UseEnvironment(Environments.Staging);

        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            if (PostgresDatabaseFixture.PostgresContainer is null)
            {
                throw new InvalidOperationException("Postgres container is not initialized.");
            }

            ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            PostgresConfig db = new()
            {
                Db = PostgresDatabaseFixture.Database!,
                Port = (ushort)PostgresDatabaseFixture.Port,
                Host = PostgresDatabaseFixture.PostgresContainer.Hostname,
                User = PostgresDatabaseFixture.Username!,
                Password = PostgresDatabaseFixture.Password!,
            };

            NpgsqlConnectionStringBuilder connectionBuilder = new()
            {
                Host = db.Host,
                Username = db.User,
                Password = db.Password,
                Database = db.Db,
                IncludeErrorDetail = true,
            };

            if (db.Port is not null)
            {
                connectionBuilder.Port = db.Port.Value;
            }

            services.AddSingleton(container =>
            {
                NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionBuilder.ConnectionString);
                dataSourceBuilder.UseNodaTime().MapEnum<UserRole>();
                return dataSourceBuilder.Build();
            });

            services.AddDbContext<ApplicationContext>((container, options) =>
            {
                NpgsqlDataSource dataSource = container.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource, o => o.UseNodaTime()).UseSnakeCaseNamingConvention();
            });

            // mock SMTP client
            services.Replace(ServiceDescriptor.Transient<ISmtpClient>(_ => new Mock<ISmtpClient>().Object));

            using IServiceScope scope = services.BuildServiceProvider().CreateScope();
            ApplicationContext dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            InitializeDatabase(dbContext);
        });
    }

    public TestApiClient CreateTestApiClient()
    {
        HttpClient client = CreateClient();
        return new TestApiClient(client);
    }

    public void InitializeDatabase()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        InitializeDatabase(dbContext);
    }

    private static void InitializeDatabase(ApplicationContext dbContext)
    {
        if (!_migrated)
        {
            dbContext.MigrateDatabase();
            _migrated = true;
            Seed(dbContext);
        }
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
    public async Task ResetDatabase()
    {
        using NpgsqlConnection conn = new(PostgresDatabaseFixture.PostgresContainer!.GetConnectionString());
        await conn.OpenAsync();

        Respawner respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            TablesToInclude = new Table[]
            {
                "users",
                "categories",
                "leaderboards",
                "account_confirmations",
                "account_recoveries",
                "runs"
            },
            DbAdapter = DbAdapter.Postgres
        });

        await respawner.ResetAsync(conn);
    }
}
