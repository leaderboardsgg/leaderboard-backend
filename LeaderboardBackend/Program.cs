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

var builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(
	ignoreExceptions: false, // Notifies of exceptions during loading of .env
	envFilePaths: new[] { "../.env" },
	trimValues: true // Trims whitespace from values
));

var exists = EnvReader.TryGetBooleanValue("USE_IN_MEMORY_DB", out bool inMemoryDb);
var useInMemoryDb = exists && inMemoryDb;

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

var app = builder.Build();

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



// Add application's Database Context to the container.
static string GetConnectionString()
{
	if (
		!EnvReader.TryGetStringValue("POSTGRES_HOST", out var host) ||
		!EnvReader.TryGetIntValue("POSTGRES_PORT", out var port) ||
		!EnvReader.TryGetStringValue("POSTGRES_USER", out var user) ||
		!EnvReader.TryGetStringValue("POSTGRES_PASSWORD", out var password) ||
		!EnvReader.TryGetStringValue("POSTGRES_DB", out var db)
	)
	{
		throw new Exception("Database env var(s) not set. Is there a .env?");
	}

	return $"Server={host};Port={port};User Id={user};Password={password};Database={db};Include Error Detail=true";
}

static void ConfigureDbContext<T>(WebApplicationBuilder builder, bool inMemoryDb) where T : DbContext
{
	builder.Services.AddDbContext<T>(opt =>
	{
		if (inMemoryDb) opt.UseInMemoryDatabase("LeaderboardBackend");
		else opt.UseNpgsql(GetConnectionString()).UseSnakeCaseNamingConvention();
	});
}
