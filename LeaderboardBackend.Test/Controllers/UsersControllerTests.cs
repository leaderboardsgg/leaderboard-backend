using LeaderboardBackend.Controllers;
using LeaderboardBackend.Models.Requests.Users;
using LeaderboardBackend.Models.Entities;
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

		_controller = new(_userServiceMock.Object, _authServiceMock.Object);
		_controller.ControllerContext = new();
		_controller.ControllerContext.HttpContext = new DefaultHttpContext();
	}

	[Test]
	public async Task GetUserById_NotFound_UserDoesNotExist()
	{
		_userServiceMock
			.Setup(x => x.GetUserById(It.IsAny<Guid>()))
			.Returns(Task.FromResult<User?>(null));

		ActionResult<User> response = await _controller.GetUserById(defaultUserId);

		Helpers.AssertResponseNotFound(response);
	}

	[Test]
	public async Task GetUserById_Ok_UserExists()
	{
		_userServiceMock
			.Setup(x => x.GetUserById(defaultUserId))
			.Returns(Task.FromResult<User?>(defaultUser));

		ActionResult<User> response = await _controller.GetUserById(defaultUserId);

		User? user = Helpers.GetValueFromObjectResult<OkObjectResult, User>(response);

		Assert.NotNull(user);
		Assert.AreEqual(defaultUser, user);
	}

	[Test]
	public async Task Register_BadRequest_PasswordsMismatch()
	{
		RegisterRequest body = new()
		{
			Username = defaultUser.Username!,
			Email = defaultUser.Email!,
			Password = "cool_password",
			PasswordConfirm = "something_different"
		};

		ActionResult<User> response = await _controller.Register(body);
		Helpers.AssertResponseBadRequest(response);
	}

	[Test]
	public async Task Register_OK_PasswordsMatchCreateSuccess()
	{
		RegisterRequest body = new()
		{
			Username = defaultUser.Username!,
			Email = defaultUser.Email!,
			Password = defaultPlaintextPassword,
			PasswordConfirm = defaultPlaintextPassword
		};

		ActionResult<User> response = await _controller.Register(body);
		User? user = Helpers.GetValueFromObjectResult<CreatedAtActionResult, User>(response);

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

		ActionResult<User> response = await _controller.Me();

		Helpers.AssertResponseForbid(response);
	}

	[Test]
	public async Task Me_Ok_UserFoundFromClaims()
	{
		_controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
		_userServiceMock
			.Setup(x => x.GetUserFromClaims(It.IsAny<ClaimsPrincipal>()))
			.Returns(Task.FromResult<User?>(defaultUser));

		ActionResult<User> response = await _controller.Me();
		User? user = Helpers.GetValueFromObjectResult<OkObjectResult, User>(response);

		Assert.NotNull(user);
		Assert.AreEqual(defaultUser, user);
	}
}
