using System;
using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models.Entities;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using LeaderboardBackend.Test.Helpers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Test.Services;

public class UserServiceTests
{
	private static IConfiguration _config = null!;
	private static ApplicationContext _context = null!;
	private static UserService _userService = null!;
	private static User _user = new User {
		Id = new Guid(),
		Username = "RageCage",
		Email = "x@y.com",
		Password = "beepboop!"
	};

	[SetUp]
	public void Setup()
	{
		_config = BuildMockConfig();
		_context = ApplicationContextFactory.CreateNewContext();
		_userService = new UserService(_context, _config);
	}

	[Test]
	public async Task CreateUser()
	{
		await _userService.CreateUser(_user);

		User? foundUser = _context.Users.Find(_user.Id);
		Assert.NotNull(foundUser);
		Assert.AreEqual(foundUser, _user);
	}

	[Test]
	public async Task GetUser_GetsAnExistingUser()
	{
		await _userService.CreateUser(_user);

		User? getUser = await _userService.GetUser(_user.Id);
		Assert.NotNull(getUser);
		Assert.AreEqual(getUser, _user);
	}

	[Test]
	public async Task GetUser_ReturnsNullForNonExistingID()
	{
		User? result = await _userService.GetUser(new Guid());
		Assert.Null(result);
	}

	[Test]
	public async Task GetUserByEmail_ReturnsUserIfEmailExists()
	{
		await _userService.CreateUser(_user);
		
		User? getUser = await _userService.GetUserByEmail(_user.Email!);
		Assert.NotNull(getUser);
		Assert.AreEqual(_user, getUser);
	}

	[Test]
	public async Task GetUserByEmail_ReturnsNullForNonExistingEmail()
	{
		User? result = await _userService.GetUserByEmail(Generators.RandomEmailAddress());
		Assert.Null(result);
	}

	[Test]
	public async Task GetUserFromClaims_ShouldGetUser()
	{
		await _userService.CreateUser(_user);
		ClaimsPrincipal principal = DefaultClaimsPrincipal();
		
		User? userFromClaims = await _userService.GetUserFromClaims(principal);
		Assert.NotNull(userFromClaims);
		Assert.AreEqual(_user, userFromClaims);
	}

	private static IConfiguration BuildMockConfig()
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

	private static ClaimsPrincipal DefaultClaimsPrincipal()
	{
		Claim[] claims = new Claim[] {
			new Claim(JwtRegisteredClaimNames.Email, _user.Email!), 
			new Claim(JwtRegisteredClaimNames.Sub, _user.Id.ToString()),
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims));
	}
}
