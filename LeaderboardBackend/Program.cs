using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
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
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using LeaderboardBackend.Swagger;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime;
using Npgsql;

#region WebApplicationBuilder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration / Options
if (!builder.Environment.IsProduction())
{
    AppConfig? appConfigWithoutDotEnv = builder.Configuration.Get<AppConfig>();
    EnvConfigurationSource dotEnvSource =
        new(new string[] { appConfigWithoutDotEnv?.EnvPath ?? ".env" }, LoadOptions.NoClobber());
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
    .AddOptions<EmailSenderConfig>()
    .BindConfiguration(EmailSenderConfig.KEY)
    .ValidateFluentValidation()
    .ValidateOnStart();

// Configure database context
builder.Services
    .AddOptions<ApplicationContextConfig>()
    .BindConfiguration(ApplicationContextConfig.KEY)
    .ValidateDataAnnotationsRecursively()
    .ValidateOnStart();

PostgresConfig db = builder.Configuration.GetSection(ApplicationContextConfig.KEY).Get<ApplicationContextConfig>()!.Pg!;

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

NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionBuilder.ConnectionString);
dataSourceBuilder.UseNodaTime().MapEnum<UserRole>();
NpgsqlDataSource dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationContext>(opt =>
{
    opt.UseNpgsql(dataSource, o => o.UseNodaTime());
    opt.UseSnakeCaseNamingConvention();
});

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAccountConfirmationService, AccountConfirmationService>();
builder.Services.AddScoped<IAccountRecoveryService, AccountRecoveryService>();
builder.Services.AddScoped<IRunService, RunService>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<ISmtpClient>(_ => new SmtpClient() { Timeout = 3000 });
builder.Services.AddSingleton<IClock>(_ => SystemClock.Instance);

AppConfig? appConfig = builder.Configuration.Get<AppConfig>();
if (!string.IsNullOrWhiteSpace(appConfig?.AllowedOrigins))
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(
            policy =>
                policy
                    .WithOrigins(appConfig.ParseAllowedOrigins())
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
        );
    });
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(
            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
        );
    });
}

// Add controllers to the container.
builder.Services
    .AddControllers(opt =>
    {
        // Enforces JSON output and causes OpenAPI UI to correctly show that we return JSON.
        opt.OutputFormatters.RemoveType<StringOutputFormatter>();
        opt.ModelBinderProviders.Insert(0, new UrlSafeBase64GuidBinderProvider());
    })
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LeaderboardBackend", Version = "v1" });

    // Enable adding XML comments to controllers to populate Swagger UI
    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

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
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                },
                Array.Empty<string>()
            }
        }
    );

    c.SupportNonNullableReferenceTypes();
    c.SchemaFilter<RequiredNotNullableSchemaFilter>();
    c.MapType<Guid>(() => new OpenApiSchema { Type = "string", Pattern = "^[a-zA-Z0-9-_]{22}$" });
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

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Configure authorisation.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        UserTypes.ADMINISTRATOR,
        policy =>
        {
            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new UserTypeRequirement(UserTypes.ADMINISTRATOR));
        }
    );
    options.AddPolicy(
        UserTypes.MODERATOR,
        policy =>
        {
            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new UserTypeRequirement(UserTypes.MODERATOR));
        }
    );

    // Handles empty [Authorize] attributes
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(new[] { JwtBearerDefaults.AuthenticationScheme })
        .RequireAuthenticatedUser()
        .AddRequirements(new[] { new UserTypeRequirement(UserTypes.USER) })
        .Build();

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(new[] { JwtBearerDefaults.AuthenticationScheme })
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IValidatorInterceptor, LeaderboardBackend.Models.Validation.UseErrorCodeInterceptor>();
builder.Services.AddFluentValidationAutoValidation(c =>
{
    c.DisableDataAnnotationsValidation = true;
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        ValidationProblemDetails problemDetails = new(context.ModelState);

        // As of this writing, we want our custom validation rules to return with
        // a 422, while keeping 400 for all other input syntax errors.
        // For JSON syntax errors that we don't override, their keys will be the field
        // path that has the error, which always starts with "$", denoting the object
        // root. We check for that, and return the error code accordingly. - zysim
        if (problemDetails.Errors.Keys.Any(x => x.StartsWith("$")))
        {
            return new BadRequestObjectResult(problemDetails);
        }

        return new UnprocessableEntityObjectResult(problemDetails);
    };
});

// Can't use AddSingleton here since we call the DB in the Handler
builder.Services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MiddlewareResultHandler>();

// Enable feature management.
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("Feature"));
#endregion

#region WebApplication
WebApplication app = builder.Build();

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
        await context.MigrateDatabaseAsync();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
#endregion

#region Accessible Program class
public partial class Program { }
#endregion
