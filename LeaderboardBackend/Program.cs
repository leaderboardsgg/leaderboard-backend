using dotenv.net;
using dotenv.net.Utilities;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Reflection;

#region WebApplicationBuilder

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(
	ignoreExceptions: false, 
	envFilePaths: new[] { builder.Configuration["EnvPath"] },
	trimValues: true // Trims whitespace from values
));

// Configure database context
bool exists = EnvReader.TryGetBooleanValue("USE_IN_MEMORY_DB", out bool inMemoryDb);
bool useInMemoryDb = exists && inMemoryDb;
ConfigureDbContext<ApplicationContext>(builder, useInMemoryDb);

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add controllers to the container.
builder.Services.AddControllers().AddJsonOptions(opt =>
{
	opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "LeaderboardBackend", Version = "v1" });

	// Enable adding XML comments to controllers to populate Swagger UI
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
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

#endregion

#region WebApplication

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardBackend v1"));
}

app.UseHttpsRedirection();

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
		opt => {
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
