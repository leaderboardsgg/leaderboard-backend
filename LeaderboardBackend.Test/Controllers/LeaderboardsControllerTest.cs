using LeaderboardBackend.Controllers;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Controllers;

public class LeaderboardTests
{
	private Mock<ILeaderboardService> _leaderboardServiceMock = null!;
	private LeaderboardsController _controller = null!;
	private static readonly Leaderboard _defaultLeaderboard = new()
	{
		Id = 1,
		Name = "Tomb 'Tomb Raider (2013)' Raider (2013) (2013)",
	};

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
			.Setup(x => x.GetLeaderboard(It.IsAny<long>()))
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
			.Setup(x => x.GetLeaderboard(It.IsAny<long>()))
			.Returns(Task.FromResult<Leaderboard?>(_defaultLeaderboard));

		ActionResult<Leaderboard> response = await _controller.GetLeaderboard(1);
		Leaderboard? leaderboard = Helpers.GetValueFromObjectResult<OkObjectResult, Leaderboard>(response);

		Assert.NotNull(leaderboard);
		Assert.AreEqual(1, leaderboard!.Id);
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
			.Setup(x => x.GetLeaderboards(It.IsAny<long[]>()))
			.Returns(Task.FromResult(mockList));

		ActionResult<List<Leaderboard>> response = await _controller.GetLeaderboards(new long[] { 1, 2 });
		List<Leaderboard>? leaderboards = Helpers.GetValueFromObjectResult<OkObjectResult, List<Leaderboard>>(response);

		Assert.NotNull(leaderboards);
		Assert.AreEqual(new ulong[] { 1, 2 }, leaderboards!.Select(l => l.Id));
	}
}
