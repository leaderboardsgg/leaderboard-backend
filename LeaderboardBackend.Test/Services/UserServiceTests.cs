using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Services;

public class UserServiceTests
{
	private static IConfiguration _mockConfig = null!;
	private static Mock<UserContext> _mockContext = null!;
	private static Mock<DbSet<User>> _mockDbSet = null!;
	private static User _user = new User {
		Id = 1,
		Username = "RageCage",
		Email = "x@y.com"
	};

	private static IConfiguration GetMockConfig()
	{
		string key = "testkeythatsatisfiesthecharacterminimum";
		string issuer = "leaderboards.gg";

		string configJson = string.Format(@"
		{{
			""Jwt"": {{
				""Key"": ""{0}"",
				""Issuer"": ""{1}""
			}}
		}}
		", key, issuer);

		return ConfigurationMockBuilder.BuildConfigurationFromJson(
			configJson
		);
	}

	[SetUp]
	public void Setup()
	{
		_mockConfig = GetMockConfig();
		_mockDbSet = new Mock<DbSet<User>>();
		var options = new DbContextOptions<UserContext>();
		_mockContext = new Mock<UserContext>(options);
		_mockContext.Setup(m => m.Users).Returns(_mockDbSet.Object);
	}

	[Test]
	public async Task CreateUser()
	{
		var service = new UserService(_mockContext.Object, _mockConfig);

		await service.CreateUser(_user);

		_mockDbSet.Verify(m => m.Add(It.IsAny<User>()), Times.Once());
		_mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once());
	}

	[Test]
	public async Task GetUser_GetsAnExistingUser()
	{
		_mockDbSet.Setup(m => m.FindAsync(It.IsAny<long>()).Result).Returns(_user);

		var service = new UserService(_mockContext.Object, _mockConfig);
		var result = await service.GetUser(1);

		Assert.NotNull(result);
		Assert.AreEqual(1, result?.Id);
		Assert.AreEqual("RageCage", result?.Username);
		_mockDbSet.Verify(m => m.FindAsync(It.IsAny<long>()), Times.Once());
	}

	[Test]
	public async Task GetUser_ReturnsNullForNonExistingID()
	{
		_mockDbSet.Setup(m => m.FindAsync(1).Result).Returns(_user);

		var service = new UserService(_mockContext.Object, _mockConfig);
		var result = await service.GetUser(2);

		Assert.Null(result);
		_mockDbSet.Verify(m => m.FindAsync((long)2), Times.Once());
	}

	// GetUserByEmail and GetUserFromClaims can't be unit tested with the current
	// strategy, as Moq is unable to mock extension methods (SingleAsync,
	// FindFirstValue).
	// Tests will need to be run against an actual database, by which we should remove
	// mocking in this file.
}
