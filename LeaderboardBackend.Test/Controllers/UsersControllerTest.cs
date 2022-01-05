
using NUnit.Framework;
using Moq;
using LeaderboardBackend.Controllers;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Test.Controllers;

public class Tests
{
	private Mock<IUserService> _userServiceMock = null!;
	private Mock<IAuthService> _authServiceMock = null!;	

	private UsersController _controller = null!;

    [SetUp]
    public void Setup()
    {
		_userServiceMock = new Mock<IUserService>();
		_authServiceMock = new Mock<IAuthService>();

		_controller = new UsersController(
			_userServiceMock.Object,
			_authServiceMock.Object
		);
    }

    [Test]
    public async Task TestGetUserNotFoundIfUserNotFound()
    {
		_userServiceMock
			.Setup(x => x.GetUser(It.IsAny<long>()))
			.Returns(Task.FromResult<User?>(null));

		ActionResult<User> response = await _controller.GetUser((long)1);

		NotFoundResult? actual = response.Result as NotFoundResult;
		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
    }
}
