using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LeaderboardBackend.Services;
using System.IdentityModel.Tokens.Jwt;
using dotenv.net;
using dotenv.net.Utilities;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(
	ignoreExceptions: false, // Notifies of exceptions during loading of .env
	envFilePaths: new[] { "../.env" },
	trimValues: true // Trims whitespace from values
));

static string GetConnectionString()
{
	string host, user, password, db;
	int port;

	if (
		!EnvReader.TryGetStringValue("POSTGRES_HOST", out host) ||
		!EnvReader.TryGetIntValue("POSTGRES_PORT", out port) ||
		!EnvReader.TryGetStringValue("POSTGRES_USER", out user) ||
		!EnvReader.TryGetStringValue("POSTGRES_PASSWORD", out password) ||
		!EnvReader.TryGetStringValue("POSTGRES_DB", out db)
	)
	{
		throw new Exception("Database env var(s) not set. Is there a .env?");
	}
	return String.Format(
		"Server={0};Port={1};User Id={2};Password={3};Database={4};Include Error Detail=true",
		host,
		port,
		user,
		password,
		db
	);
}

// Add services to the container.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddDbContext<UserContext>(opt => opt.UseInMemoryDatabase(DbName));
builder.Services.AddDbContext<LeaderboardContext>(opt => opt.UseInMemoryDatabase(DbName));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "LeaderboardBackend", Version = "v1" });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
