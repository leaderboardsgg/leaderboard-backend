using LeaderboardBackend.Controllers;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using LeaderboardBackend.Services.Impl;
using LeaderboardBackend.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Controllers;

internal class RunsControllerTest
{
	private RunsController _controller = null!;

	[SetUp]
	public void Setup()
	{
		RunService service = new RunService(ApplicationContextFactory.CreateNewContext());
		_controller = new RunsController(service);
	}

	[Test]
	public async Task GetRun_NotFound_WhenNotExist()
	{
		ActionResult<Run> response = await _controller.GetRun(new Guid());
		ObjectResultHelpers.AssertResponseNotFound(response);
	}
}
