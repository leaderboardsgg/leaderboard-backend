using LeaderboardBackend.Controllers;
using LeaderboardBackend.Controllers.Requests;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.Controllers;

public class UsersControllerTests
{
	private UsersController _controller = null!;
	private Mock<IUserService> _userServiceMock = null!;
	private Mock<IAuthService> _authServiceMock = null!;

	private static readonly string defaultPlaintextPassword = "beepboop";
	private static readonly Guid defaultUserId = Guid.NewGuid();
	private static readonly User defaultUser = new()
	{
		Username = "RageCage",
		Email = "x@y.com",
		Password = BCryptNet.EnhancedHashPassword(defaultPlaintextPassword),
	};

	[SetUp]
	public void Setup()
	{
		_userServiceMock = new Mock<IUserService>();
		_authServiceMock = new Mock<IAuthService>();

		_controller = new(
			_userServiceMock.Object,
			_authServiceMock.Object
		);

		_controller.ControllerContext = new();
		_controller.ControllerContext.HttpContext = new DefaultHttpContext();
	}

	[Test]
	public async Task GetUser_NotFound_UserDoesNotExist()
	{
		_userServiceMock
			.Setup(x => x.GetUser(It.IsAny<Guid>()))
			.Returns(Task.FromResult<User?>(null));

		var response = await _controller.GetUser(defaultUserId);
		Helpers.AssertResponseNotFound(response);
	}

	[Test]
	public async Task GetUser_Ok_UserExists()
	{
		_userServiceMock
			.Setup(x => x.GetUser(defaultUserId))
			.Returns(Task.FromResult<User?>(defaultUser));

		var response = await _controller.GetUser(defaultUserId);
		var user = Helpers.GetValueFromObjectResult<OkObjectResult, User>(response);

		Assert.NotNull(user);
		Assert.AreEqual(defaultUser, user);
	}

	[Test]
	public async Task Register_BadRequest_PasswordsMismatch()
	{
		var body = new RegisterRequest
		{
			Username = defaultUser.Username!,
			Email = defaultUser.Email!,
			Password = "cool_password",
			PasswordConfirm = "something_different"
		};

		var response = await _controller.Register(body);
		Helpers.AssertResponseBadRequest(response);
	}

	[Test]
	public async Task Register_OK_PasswordsMatchCreateSuccess()
	{
		var body = new RegisterRequest
		{
			Username = defaultUser.Username!,
			Email = defaultUser.Email!,
			Password = defaultPlaintextPassword,
			PasswordConfirm = defaultPlaintextPassword
		};

		var response = await _controller.Register(body);
		var user = Helpers.GetValueFromObjectResult<CreatedAtActionResult, User>(response);

		Assert.NotNull(user);
		Assert.AreEqual(defaultUser.Username, user!.Username);
		Assert.AreEqual(defaultUser.Email, user!.Email);

		// This route creates a new user, and thus does a new password hash.
		// Since hashing the password again won't produce the same hash as
		// defaultUser, we do a cryptographic verify instead.
		Assert.True(BCryptNet.EnhancedVerify(defaultPlaintextPassword, user!.Password));
	}

	[Test]
	public async Task Me_Forbid_NoUserInClaims()
	{
		_controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
		_userServiceMock
			.Setup(x => x.GetUserFromClaims(It.IsAny<ClaimsPrincipal>()))
			.Returns(Task.FromResult<User?>(null));

		var response = await _controller.Me();

		Helpers.AssertResponseForbid(response);
	}

	[Test]
	public async Task Me_Ok_UserFoundFromClaims()
	{
		_controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
		_userServiceMock
			.Setup(x => x.GetUserFromClaims(It.IsAny<ClaimsPrincipal>()))
			.Returns(Task.FromResult<User?>(defaultUser));

		var response = await _controller.Me();
		var user = Helpers.GetValueFromObjectResult<OkObjectResult, User>(response);

		Assert.NotNull(user);
		Assert.AreEqual(defaultUser, user);
	}
}
