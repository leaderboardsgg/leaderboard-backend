using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using DotNetEnv.Configuration;
using FluentValidation;
using FluentValidation.AspNetCore;
using LeaderboardBackend;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Converters;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using MicroElements.Swashbuckle.NodaTime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Npgsql;
using Zomp.EFCore.WindowFunctions.Npgsql;

#region WebApplicationBuilder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration / Options
if (!builder.Environment.IsProduction())
{
    AppConfig? appConfigWithoutDotEnv = builder.Configuration.Get<AppConfig>();
    EnvConfigurationSource dotEnvSource =
        new([appConfigWithoutDotEnv?.EnvPath ?? ".env"], LoadOptions.NoClobber());
    builder.Configuration.Sources.Insert(0, dotEnvSource); // all other configuration providers override .env
}

// add all FluentValidation validators
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services
    .AddOptions<AppConfig>()
    .Bind(builder.Configuration)
    .ValidateFluentValidation()
    .ValidateOnStart();

builder.Services
    .AddOptions<BrevoOptions>()
    .BindConfiguration(BrevoOptions.KEY)
    .ValidateFluentValidation()
    .ValidateOnStart();

// Configure database context
builder.Services
    .AddOptions<ApplicationContextConfig>()
    .BindConfiguration(ApplicationContextConfig.KEY)
    .ValidateDataAnnotationsRecursively()
    .ValidateOnStart();

#region DBContext

builder.Services.AddDbContext<ApplicationContext>(
    (services, opt) =>
    {
        ApplicationContextConfig appConfig = services
            .GetRequiredService<IOptions<ApplicationContextConfig>>()
            .Value;
        if (appConfig.Pg is not null)
        {
            PostgresConfig db = appConfig.Pg;
            NpgsqlConnectionStringBuilder connectionBuilder =
                new()
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

            opt.UseNpgsql(connectionBuilder.ConnectionString, o =>
            {
                o.MapEnum<UserRole>();
                o.MapEnum<SortDirection>();
                o.MapEnum<RunType>();
                o.UseNodaTime();
                o.UseWindowFunctions();
            });

            opt.UseSnakeCaseNamingConvention();
            opt.UseValidationCheckConstraints();

            if (builder.Environment.IsDevelopment())
            {
                opt.UseSeeding((context, _) =>
                {
                    User admin = context.Set<User>().SingleOrDefault(u => u.Email == "admin@leaderboards.gg")
                        ?? context.Add(new User()
                        {
                            Email = "admin@leaderboards.gg",
                            Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                            Username = "admin",
                            Role = UserRole.Administrator,
                        }).Entity;

                    User user = context.Set<User>().SingleOrDefault(u => u.Email == "user1@leaderboards.gg")
                        ?? 
                        context.Add(new User()
                        {
                            Email = "user1@leaderboards.gg",
                            Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                            Username = "user1",
                            Role = UserRole.Confirmed,
                        }).Entity;

                    if (context.Set<Leaderboard>().SingleOrDefault(b => b.Slug == "mario-64") is null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "Mario 64",
                            Slug = "mario-64",
                            Info = "Jump Man wahoos in 3D for the first time.",
                            Categories = [
                                new()
                                {
                                    Name = "120 Stars",
                                    Slug = "120-stars",
                                    SortDirection = SortDirection.Ascending,
                                    Type = RunType.Time,
                                    Runs = [
                                        new()
                                        {
                                            Info = "WR!!!",
                                            User = admin!,
                                            TimeOrScore = Duration.FromMinutes(90).ToInt64Nanoseconds(),
                                            PlayedOn = new(2026, 1, 4)
                                        },
                                        new()
                                        {
                                            Info = "Really good run. Will definitely improve on this soon.",
                                            User = user!,
                                            TimeOrScore = Duration.FromMinutes(100).ToInt64Nanoseconds(),
                                            PlayedOn = new(2025, 5, 25)
                                        },
                                    ]
                                },
                                new()
                                {
                                    Name = "Rainbow Ride Coins",
                                    Slug = "rainbow-ride-coins",
                                    SortDirection = SortDirection.Descending,
                                    Type = RunType.Score,
                                    Info = "Get as many coins as you can in rainbow ride. Only the number the game saves actually counts.",
                                    Runs = [
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 146,
                                            PlayedOn = new(2021, 7, 2)
                                        },
                                        new()
                                        {
                                            User = user,
                                            TimeOrScore = 255,
                                            Info = "I used cloning to get this score.",
                                            PlayedOn = new(2023, 8, 19)
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    if (context.Set<Leaderboard>().SingleOrDefault(b => b.Slug == "worlds-hardest-game") is null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "The World's Hardest Game",
                            Slug = "worlds-hardest-game",
                            Categories = [
                                new()
                                {
                                    Name = "Beat the Game (Fewest Deaths)",
                                    Slug = "fewest-deaths",
                                    SortDirection = SortDirection.Ascending,
                                    Type = RunType.Score,
                                    Info = "Beat the game while dying as few times as you can. Time doesn't matter.",
                                    Runs = [
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 1,
                                            PlayedOn = new(2008, 6, 7),
                                            Info = "So close!"
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 0,
                                            PlayedOn = new(2020, 12, 5),
                                            Info = "Finally conquered this game from my childhood."
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    if (context.Set<Leaderboard>().SingleOrDefault(b => b.Slug == "call-of-duty-black-ops") is null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "Call of Duty: Black Ops",
                            Slug = "call-of-duty-black-ops",
                            Categories = [
                                new()
                                {
                                    Name = "Kino der Toten (High Round, Solo)",
                                    Slug = "kino-highround-solo",
                                    Info = "Reach the highest round you can on Kino der Toten. Singleplayer only.",
                                    SortDirection = SortDirection.Descending,
                                    Type = RunType.Score,
                                    Runs = [
                                        new()
                                        {
                                            User = user,
                                            TimeOrScore = 50,
                                            Info = "Could have gone higher but I got bored.",
                                            PlayedOn = new(2019, 7, 15)
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 170,
                                            PlayedOn = new(2022, 11, 11)
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    context.SaveChanges();
                }).UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    User admin = await context.Set<User>().SingleOrDefaultAsync(u => u.Email == "admin@leaderboards.gg", cancellationToken)
                        ?? context.Add(new User()
                        {
                            Email = "admin@leaderboards.gg",
                            Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                            Username = "admin",
                            Role = UserRole.Administrator,
                        }).Entity;

                    User user = await context.Set<User>().SingleOrDefaultAsync(u => u.Email == "user1@leaderboards.gg", cancellationToken)
                        ?? context.Add(new User()
                        {
                            Email = "user1@leaderboards.gg",
                            Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                            Username = "user1",
                            Role = UserRole.Confirmed,
                        }).Entity;

                    Leaderboard? board1 = await context.Set<Leaderboard>().SingleOrDefaultAsync(b => b.Slug == "mario-64", cancellationToken);

                    if (board1 == null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "Mario 64",
                            Slug = "mario-64",
                            Info = "Jump Man wahoos in 3D for the first time.",
                            Categories = [
                                new()
                                {
                                    Name = "120 Stars",
                                    Slug = "120-stars",
                                    SortDirection = SortDirection.Ascending,
                                    Type = RunType.Time,
                                    Runs = [
                                        new()
                                        {
                                            Info = "WR!!!",
                                            User = admin,
                                            TimeOrScore = Duration.FromMinutes(90).ToInt64Nanoseconds(),
                                            PlayedOn = new(2026, 1, 4)
                                        },
                                        new()
                                        {
                                            Info = "Really good run. Will definitely improve on this soon.",
                                            User = user,
                                            TimeOrScore = Duration.FromMinutes(100).ToInt64Nanoseconds(),
                                            PlayedOn = new(2025, 5, 25)
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 255,
                                            PlayedOn = new(2023, 8, 24)
                                        }
                                    ]
                                },
                                new()
                                {
                                    Name = "Rainbow Ride Coins",
                                    Slug = "rainbow-ride-coins",
                                    SortDirection = SortDirection.Descending,
                                    Type = RunType.Score,
                                    Info = "Get as many coins as you can in rainbow ride. Only the number the game saves actually counts.",
                                    Runs = [
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 146,
                                            PlayedOn = new(2021, 7, 2)
                                        },
                                        new()
                                        {
                                            User = user,
                                            TimeOrScore = 255,
                                            Info = "I used cloning to get this score.",
                                            PlayedOn = new(2023, 8, 19)
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 255,
                                            PlayedOn = new(2023, 8, 24)
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    Leaderboard? board2 = await context.Set<Leaderboard>().SingleOrDefaultAsync(b => b.Slug == "worlds-hardest-game", cancellationToken);

                    if (board2 is null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "The World's Hardest Game",
                            Slug = "worlds-hardest-game",
                            Categories = [
                                new()
                                {
                                    Name = "Beat the Game (Fewest Deaths)",
                                    Slug = "fewest-deaths",
                                    SortDirection = SortDirection.Ascending,
                                    Type = RunType.Score,
                                    Info = "Beat the game while dying as few times as you can. Time doesn't matter.",
                                    Runs = [
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 1,
                                            PlayedOn = new(2008, 6, 7),
                                            Info = "So close!"
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 0,
                                            PlayedOn = new(2020, 12, 5),
                                            Info = "Finally conquered this game from my childhood."
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    Leaderboard? board3 = await context.Set<Leaderboard>().SingleOrDefaultAsync(b => b.Slug == "call-of-duty-black-ops", cancellationToken);

                    if (board3 is null)
                    {
                        context.Add(new Leaderboard()
                        {
                            Name = "Call of Duty: Black Ops",
                            Slug = "call-of-duty-black-ops",
                            Categories = [
                                new()
                                {
                                    Name = "Kino der Toten (High Round, Solo)",
                                    Slug = "kino-highround-solo",
                                    Info = "Reach the highest round you can on Kino der Toten. Singleplayer only.",
                                    SortDirection = SortDirection.Descending,
                                    Type = RunType.Score,
                                    Runs = [
                                        new()
                                        {
                                            User = user,
                                            TimeOrScore = 50,
                                            Info = "Could have gone higher but I got bored.",
                                            PlayedOn = new(2019, 7, 15)
                                        },
                                        new()
                                        {
                                            User = admin,
                                            TimeOrScore = 170,
                                            PlayedOn = new(2022, 11, 11)
                                        }
                                    ]
                                }
                            ]
                        });
                    }

                    await context.SaveChangesAsync(cancellationToken);
                });
            }
        }
        else
        {
            throw new UnreachableException(
                "The database configuration is invalid but it was not caught by validation!"
            );
        }
    }
);

#endregion

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAccountConfirmationService, AccountConfirmationService>();
builder.Services.AddScoped<IAccountRecoveryService, AccountRecoveryService>();
builder.Services.AddScoped<IRunService, RunService>();
builder.Services.AddSingleton<IEmailSender, BrevoService>();
builder.Services.AddSingleton<IClock>(_ => SystemClock.Instance);

AppConfig? appConfig = builder.Configuration.Get<AppConfig>();
if (!string.IsNullOrWhiteSpace(appConfig?.AllowedOrigins))
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(
            policy =>
                policy
                    .WithOrigins(appConfig.ParseAllowedOrigins())
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
        ));
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(
            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
        ));
}

JsonSerializerOptions jsonSerializerOptions = new();

// Add controllers to the container.
builder.Services
    .AddControllers(opt =>
    {
        // Enforces JSON output and causes OpenAPI UI to correctly show that we return JSON.
        opt.OutputFormatters.RemoveType<StringOutputFormatter>();
        opt.ModelBinderProviders.Insert(0, new UrlSafeBase64GuidBinderProvider());
        opt.Filters.AddService<ValidationFilter>();
    })
    .ConfigureApiBehaviorOptions(opt => opt.SuppressModelStateInvalidFilter = true)
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(null, false));
        opt.JsonSerializerOptions.Converters.Add(new GuidJsonConverter());
        opt.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        jsonSerializerOptions = opt.JsonSerializerOptions;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LeaderboardBackend", Version = "v1" });

    // Enable adding XML comments to controllers to populate Swagger UI
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.EnableAnnotations(true, true);
    c.UseAllOfToExtendReferenceSchemas();

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JWT Authorization using the Bearer scheme",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = JwtBearerDefaults.AuthenticationScheme
        }
    );
    c.AddSecurityRequirement(document =>
        new()
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        }
    );

    c.SupportNonNullableReferenceTypes();
    c.MapType<Guid>(() => new OpenApiSchema { Type = JsonSchemaType.String, Pattern = "^[a-zA-Z0-9-_]{22}$" });
    c.ConfigureForNodaTimeWithSystemTextJson(jsonSerializerOptions, null, null, true, new(DateTimeZoneProviders.Tzdb)
    {
        Instant = Instant.FromUtc(1984, 1, 1, 0, 0),
        ZonedDateTime = ZonedDateTime.FromDateTimeOffset(new(new DateTime(2000, 1, 1)))
    });
});

// Configure JWT Authentication.
builder.Services
    .AddOptions<JwtConfig>()
    .BindConfiguration(JwtConfig.KEY)
    .ValidateDataAnnotationsRecursively()
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtConfig>>(
        (opt, jwtConfig) =>
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Value.Issuer,
                ValidAudience = jwtConfig.Value.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtConfig.Value.Key)
                )
            }
    );

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(UserTypes.ADMINISTRATOR, policy =>
        {
            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new UserTypeRequirement(UserTypes.ADMINISTRATOR));
        }
)
    .AddPolicy(UserTypes.MODERATOR, policy =>
        {
            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new UserTypeRequirement(UserTypes.MODERATOR));
        }
)
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .AddRequirements([new UserTypeRequirement(UserTypes.USER)])
        .Build());

builder.Services.AddSingleton<IValidatorInterceptor, LeaderboardBackend.Models.Validation.UseErrorCodeInterceptor>();
builder.Services.AddFluentValidationAutoValidation(c => c.DisableDataAnnotationsValidation = true);

// Can't use AddSingleton here since we call the DB in the Handler
builder.Services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MiddlewareResultHandler>();
builder.Services.AddSingleton<ValidationFilter>();

// Enable feature management.
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Feature"));

builder.Services.AddHttpContextAccessor();
#endregion

#region WebApplication
WebApplication app = builder.Build();

BrevoOptions brevoOptions = app.Services.GetRequiredService<IOptionsMonitor<BrevoOptions>>().CurrentValue;
brevo_csharp.Client.Configuration.Default.AddApiKey("api-key", brevoOptions.ApiKey);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardBackend v1"));

// Database creation / migration
using (IServiceScope scope = app.Services.CreateScope())
using (ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>())
{
    ApplicationContextConfig config = scope.ServiceProvider
        .GetRequiredService<IOptions<ApplicationContextConfig>>()
        .Value;

    if (args.Contains("--migrate-db")) // the only way to migrate a production database
    {
        context.Database.Migrate();
        return;
    }

    if (config.MigrateDb && app.Environment.IsDevelopment())
    {
        // migration as part of the startup phase (dev env only)
        context.Database.Migrate();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
#endregion
