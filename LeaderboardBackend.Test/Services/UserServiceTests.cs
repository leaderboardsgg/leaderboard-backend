using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Services;

public class UserServiceTests
{
	private static Guid _userId = Guid.Parse("44818346-23b7-4f96-87e0-6edcabd4244c");
	private static UserService _service = null!;
	private static User _testUser = new User
	{
		Id = _userId,
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

	private static UserContext GetUserContext()
	{
		return new UserContext(
			new DbContextOptionsBuilder<UserContext>().UseNpgsql(
				"Host=localhost;Port=5433;Database=leaderboardstest;Username=admin;Password=example;"
			).Options
		);
	}

	[SetUp]
	public void Setup()
	{
		_service = new UserService(GetUserContext(), GetMockConfig());
	}

	[Test]
	public async Task CreateUser()
	{
		// TODO: the DB call gives "System.IO.EndOfStreamException: Attempted to read past the end of the stream"
		await _service.CreateUser(_testUser);
		Assert.NotNull(await _service.GetUser(_userId));

		// TODO: Call this too
		// _mockDbSet.Verify(m => m.Add(It.IsAny<User>()), Times.Once());
		// _mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once());
	}

	[Test]
	public async Task GetUser_GetsAnExistingUser()
	{
		var result = await _service.GetUser(_userId);

		Assert.NotNull(result);
		Assert.AreEqual(_userId, result?.Id);
		Assert.AreEqual("RageCage", result?.Username);
	}

	[Test]
	public async Task GetUser_ReturnsNullForNonExistingID()
	{
		var result = await _service.GetUser(Guid.NewGuid());

		Assert.Null(result);
	}

	// TODO: Implement this + fail states
	// [Test]
	// public async Task GetUserByEmail_GetsUser()
	// {
	// }

	// TODO: Implement this + fail states
	// [Test]
	// public async Task GetUserFromClaims_GetsUser()
	// {
	// }
}
