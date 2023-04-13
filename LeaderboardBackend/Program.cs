using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using DotNetEnv.Configuration;
using FluentValidation;
using LeaderboardBackend;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using BCryptNet = BCrypt.Net.BCrypt;

#region WebApplicationBuilder
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration / Options
if (!builder.Environment.IsProduction())
{
	AppConfig? appConfigWithoutDotEnv = builder.Configuration.Get<AppConfig>();
	EnvConfigurationSource dotEnvSource = new(new string[] { appConfigWithoutDotEnv?.EnvPath ?? ".env" }, LoadOptions.NoClobber());
	builder.Configuration.Sources.Insert(0, dotEnvSource); // all other configuration providers override .env
}

// add all FluentValidation validators
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddOptions<AppConfig>()
	.Bind(builder.Configuration)
	.ValidateFluentValidation()
	.ValidateOnStart();

// Configure database context
builder.Services.AddOptions<ApplicationContextConfig>()
	.BindConfiguration(ApplicationContextConfig.KEY)
	.ValidateDataAnnotationsRecursively()
	.ValidateOnStart();

builder.Services.AddDbContext<ApplicationContext>((services, opt) =>
{
	ApplicationContextConfig appConfig = services.GetRequiredService<IOptions<ApplicationContextConfig>>().Value;
	if (appConfig.UseInMemoryDb)
	{
		opt.UseInMemoryDatabase("LeaderboardBackend");
	}
	else if (appConfig.Pg is not null)
	{
		PostgresConfig db = appConfig.Pg;
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

		opt.UseNpgsql(connectionBuilder.ConnectionString, o => o.UseNodaTime());
		opt.UseSnakeCaseNamingConvention();
	}
	else
	{
		throw new UnreachableException("The database configuration is invalid but it was not caught by validation!");
	}
});

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IModshipService, ModshipService>();
builder.Services.AddScoped<IParticipationService, ParticipationService>();
builder.Services.AddScoped<IJudgementService, JudgementService>();
builder.Services.AddScoped<IRunService, RunService>();
builder.Services.AddScoped<IBanService, BanService>();

AppConfig? appConfig = builder.Configuration.Get<AppConfig>();
if (!string.IsNullOrWhiteSpace(appConfig?.AllowedOrigins))
{
	builder.Services.AddCors(options =>
	{
		options.AddDefaultPolicy(policy => policy
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
		options.AddDefaultPolicy(policy => policy
			.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader()
		);
	});
}

// Add controllers to the container.
builder.Services.AddControllers(opt =>
{
	// Enforces JSON output and causes OpenAPI UI to correctly show that we return JSON.
	opt.OutputFormatters.RemoveType<StringOutputFormatter>();
}).AddJsonOptions(opt =>
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
});

// Configure JWT Authentication.
builder.Services.AddOptions<JwtConfig>()
	.BindConfiguration(JwtConfig.KEY)
	.ValidateDataAnnotationsRecursively()
	.ValidateOnStart();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
	.Configure<IOptions<JwtConfig>>((opt, jwtConfig) => opt.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtConfig.Value.Issuer,
		ValidAudience = jwtConfig.Value.Issuer,
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Value.Key))
	});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer();

// Configure authorisation.
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(UserTypes.ADMINISTRATOR, policy =>
	{
		policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
		policy.RequireAuthenticatedUser();
		policy.Requirements.Add(new UserTypeRequirement(UserTypes.ADMINISTRATOR));
	});
	options.AddPolicy(UserTypes.MODERATOR, policy =>
	{
		policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
		policy.RequireAuthenticatedUser();
		policy.Requirements.Add(new UserTypeRequirement(UserTypes.MODERATOR));
	});

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

// Can't use AddSingleton here since we call the DB in the Handler
builder.Services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MiddlewareResultHandler>();
#endregion

#region WebApplication
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardBackend v1"));
}

// Database creation / migration
using (IServiceScope scope = app.Services.CreateScope())
using (ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>())
{
	ApplicationContextConfig config = scope.ServiceProvider
		.GetRequiredService<IOptions<ApplicationContextConfig>>().Value;

	if (args.Contains("--migrate-db")) // the only way to migrate a production database
	{
		if (!config.UseInMemoryDb)
		{
			context.Database.Migrate();
		}

		return;
	}

	if (config.UseInMemoryDb)
	{
		context.Database.EnsureCreated();
		User? defaultUser = context.Find<User>(ApplicationContext.s_SeedAdminId);
		if (defaultUser is null)
		{
			throw new InvalidOperationException("The default user was not correctly seeded.");
		}

		defaultUser.Username = Environment.GetEnvironmentVariable("LGG_ADMIN_USERNAME") ?? defaultUser.Username;
		defaultUser.Email = Environment.GetEnvironmentVariable("LGG_ADMIN_EMAIL") ?? defaultUser.Email;
		string? newPassword = Environment.GetEnvironmentVariable("LGG_ADMIN_PASSWORD");
		if (newPassword is not null)
		{
			defaultUser.Password = BCryptNet.EnhancedHashPassword(newPassword);
		}
		context.SaveChanges();
	}
	else if (config.MigrateDb && app.Environment.IsDevelopment())
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

#region Accessible Program class
public partial class Program { }
#endregion
