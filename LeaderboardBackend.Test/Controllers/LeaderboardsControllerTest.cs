
using NUnit.Framework;
using Moq;
using LeaderboardBackend.Controllers;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Test.Controllers;

public class LeaderboardTests
{
	private Mock<ILeaderboardService> _leaderboardServiceMock = null!;

	private LeaderboardsController _controller = null!;

    [SetUp]
    public void Setup()
    {
		_leaderboardServiceMock = new Mock<ILeaderboardService>();

		_controller = new LeaderboardsController(
			_leaderboardServiceMock.Object
		);
    }

    [Test]
    public async Task TestGetUserNotFoundIfUserNotFound()
    {
		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboard(It.IsAny<long>()))
			.Returns(Task.FromResult<Leaderboard?>(null));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard((long)1);

		NotFoundResult? actual = response.Result as NotFoundResult;
		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
    }
}
