
using NUnit.Framework;
using Moq;
using LeaderboardBackend.Controllers;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
	public async Task GetLeaderboard_NotFound_LeaderboardDoesNotExist()
	{
		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboard(It.IsAny<long>()))
			.Returns(Task.FromResult<Leaderboard?>(null));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard((long)1);

		NotFoundResult? actual = response.Result as NotFoundResult;
		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
	}

	[Test]
	public async Task GetLeaderboard_Ok_LeaderboardExists()
	{
		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboard(It.IsAny<long>()))
			.Returns(Task.FromResult<Leaderboard?>(new Leaderboard { Id = 1 }));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard((long)1);

		Assert.NotNull(response.Value);
		Assert.AreEqual(1, response.Value?.Id);
	}

	[Test]
	public async Task GetLeaderboards_Ok_ListExists()
	{
		List<Leaderboard> mockList = new List<Leaderboard> {
			new Leaderboard { Id = 1 },
			new Leaderboard { Id = 2 },
		};

		_leaderboardServiceMock
			.Setup(x => x.GetLeaderboards(It.IsAny<long[]>()))
			.Returns(Task.FromResult<List<Leaderboard>>(mockList));

		ActionResult<List<Leaderboard>> response = await _controller.GetLeaderboards(new long[] {1, 2});

		Assert.AreEqual(new long[] {1,2}, response.Value?.ConvertAll(l => l.Id));
	}
}
