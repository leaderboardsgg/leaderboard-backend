using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using dotenv.net;
using dotenv.net.Utilities;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCryptNet = BCrypt.Net.BCrypt;

#region WebApplicationBuilder

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(
	ignoreExceptions: false,
	overwriteExistingVars: false,
	envFilePaths: new[] { builder.Configuration["EnvPath"] },
	trimValues: true
));

// Configure database context
bool exists = EnvReader.TryGetBooleanValue("USE_IN_MEMORY_DB", out bool inMemoryDb);
bool useInMemoryDb = exists && inMemoryDb;
ConfigureDbContext<ApplicationContext>(builder, useInMemoryDb);

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
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Issuer"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
		};
	});

// Configure authorisation.
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(UserTypes.Admin, policy =>
	{
		policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
		policy.RequireAuthenticatedUser();
		policy.Requirements.Add(new UserTypeRequirement(UserTypes.Admin));
	});
	options.AddPolicy(UserTypes.Mod, policy =>
	{
		policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
		policy.RequireAuthenticatedUser();
		policy.Requirements.Add(new UserTypeRequirement(UserTypes.Mod));
	});

	// Handles empty [Authorize] attributes
	options.DefaultPolicy = new AuthorizationPolicyBuilder()
		.AddAuthenticationSchemes(new[] { JwtBearerDefaults.AuthenticationScheme })
		.RequireAuthenticatedUser()
		.AddRequirements(new[] { new UserTypeRequirement(UserTypes.User) })
		.Build();

	options.FallbackPolicy = new AuthorizationPolicyBuilder()
		.AddAuthenticationSchemes(new[] { JwtBearerDefaults.AuthenticationScheme })
		.RequireAuthenticatedUser()
		.Build();
});

// Can't use AddSingleton here since we call the DB in the Handler
builder.Services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();

#endregion

#region WebApplication

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardBackend v1"));
}

// If in memory DB, the only way to have an admin user is to seed it at startup.
if (inMemoryDb)
{
	using IServiceScope scope = app.Services.CreateScope();
	ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

	User admin = new()
	{
		Username = "Galactus",
		Email = "omega@star.com",
		Password = BCryptNet.EnhancedHashPassword("3ntr0pyChaos"),
		Admin = true,
	};

	context.Users.Add(admin);
	await context.SaveChangesAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

#endregion

#region Helpers

// Add application's Database Context to the container.
static string GetConnectionString(WebApplicationBuilder builder)
{
	string portVar = builder.Configuration["Db:PortVar"];
	if (
		!EnvReader.TryGetStringValue("POSTGRES_HOST", out string host) ||
		!EnvReader.TryGetStringValue("POSTGRES_USER", out string user) ||
		!EnvReader.TryGetStringValue("POSTGRES_PASSWORD", out string password) ||
		!EnvReader.TryGetStringValue("POSTGRES_DB", out string db) ||
		!EnvReader.TryGetIntValue(portVar, out int port)
	)
	{
		throw new Exception("Database env var(s) not set. Is there a .env?");
	}
	return $"Server={host};Port={port};User Id={user};Password={password};Database={db};Include Error Detail=true";
}

// Configure a Database context, configuring based on the USE_IN_MEMORY_DATABASE environment variable.
static void ConfigureDbContext<T>(WebApplicationBuilder builder, bool inMemoryDb) where T : DbContext
{
	builder.Services.AddDbContext<T>(
		opt =>
		{
			if (inMemoryDb)
			{
				opt.UseInMemoryDatabase("LeaderboardBackend");
			} else
			{
				opt.UseNpgsql(
					GetConnectionString(builder)
				).UseSnakeCaseNamingConvention();
			}
		}
	);
}

#endregion

#region Accessible Program class

public partial class Program { }

#endregion
