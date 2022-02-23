using LeaderboardBackend.Controllers;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Controllers;

public class LeaderboardTests
{
	private Mock<ILeaderboardService> _leaderboardServiceMock = null!;
	private LeaderboardsController _controller = null!;

	[SetUp]
	public void Setup()
	{
		_leaderboardServiceMock = new Mock<ILeaderboardService>();

		_controller = new LeaderboardsController(_leaderboardServiceMock.Object);
	}

	[Test]
	public async Task GetLeaderboard_NotFound_LeaderboardDoesNotExist()
	{
		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboard(It.IsAny<ulong>()))
			.Returns(Task.FromResult<Leaderboard?>(null));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard(1);
		var actual = response.Result as NotFoundResult;

		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
	}

	[Test]
	public async Task GetLeaderboard_Ok_LeaderboardExists()
	{
		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboard(It.IsAny<ulong>()))
			.Returns(Task.FromResult<Leaderboard?>(new Leaderboard { Id = 1 }));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard(1);

		Assert.NotNull(response.Value);
		Assert.AreEqual(1, response.Value?.Id);
	}

	[Test]
	public async Task GetLeaderboards_Ok_ListExists()
	{
		List<Leaderboard> mockList = new()
		{
			new() { Id = 1 },
			new() { Id = 2 }
		};

		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboards(It.IsAny<ulong[]>()))
			.Returns(Task.FromResult(mockList));

		ActionResult<List<Leaderboard>> response = await _controller.GetLeaderboards(new ulong[] { 1, 2 });

		Assert.AreEqual(new ulong[] { 1, 2 }, response.Value?.ConvertAll(l => l.Id));
	}
}
