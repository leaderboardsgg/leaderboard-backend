using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
{
	private static TestApiClient s_apiClient = null!;
	private static TestApiFactory s_factory = null!;
	private static string? s_jwt;

	private readonly Faker<CreateLeaderboardRequest> _createBoardReqFaker = new AutoFaker<CreateLeaderboardRequest>()
		.RuleFor(x => x.Slug, b => string.Join('-', b.Lorem.Words(2)));

	[OneTimeSetUp]
	public async Task OneTimeSetUp()
	{
		s_factory = new TestApiFactory();
		s_apiClient = s_factory.CreateTestApiClient();

		s_factory.ResetDatabase();
		s_jwt = (await s_apiClient.LoginAdminUser()).Token;
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		s_factory.Dispose();
	}

	[Test]
	public void GetLeaderboard_NotFound()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_apiClient.Get<LeaderboardViewModel>($"/api/leaderboards/{long.MaxValue}", new()))!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public async Task CreateLeaderboard_GetLeaderboard_OK()
	{
		CreateLeaderboardRequest req = _createBoardReqFaker.Generate();
		LeaderboardViewModel createdLeaderboard = await s_apiClient.Post<LeaderboardViewModel>(
			"/api/leaderboards",
			new()
			{
				Body = req,
				Jwt = s_jwt
			});

		LeaderboardViewModel retrievedLeaderboard = await s_apiClient.Get<LeaderboardViewModel>(
			$"/api/leaderboards/{createdLeaderboard?.Id}",
			new());

		createdLeaderboard.Should().NotBeNull();
		(string, string) expectedCreatedBoard = ValueTuple.Create(req.Name, req.Slug);
		(string, string) actualCreatedBoard = ValueTuple.Create(createdLeaderboard!.Name, createdLeaderboard.Slug);
		expectedCreatedBoard.Should().BeEquivalentTo(actualCreatedBoard);

		createdLeaderboard.Should().BeEquivalentTo(retrievedLeaderboard);
	}

	[Test]
	public async Task CreateLeaderboards_GetLeaderboards()
	{
		IEnumerable<Task<LeaderboardViewModel>> boardCreationTasks = _createBoardReqFaker.GenerateBetween(3, 10)
			.Select(req => s_apiClient.Post<LeaderboardViewModel>(
				"/api/leaderboards",
				new()
				{
					Body = req,
					Jwt = s_jwt
				}));
		LeaderboardViewModel[] createdLeaderboards = await Task.WhenAll(boardCreationTasks);

		IEnumerable<long> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
		string leaderboardIdQuery = ListToQueryString(leaderboardIds, "ids");

		List<LeaderboardViewModel> leaderboards = await s_apiClient.Get<List<LeaderboardViewModel>>(
			$"api/leaderboards?{leaderboardIdQuery}",
			new());

		leaderboards.Should().BeEquivalentTo(createdLeaderboards);
	}

	[Test]
	public async Task GetLeaderboards_BySlug_OK()
	{
		CreateLeaderboardRequest createReqBody = _createBoardReqFaker.Generate();
		LeaderboardViewModel createdLeaderboard = await s_apiClient.Post<LeaderboardViewModel>(
			"/api/leaderboards",
			new()
			{
				Body = createReqBody,
				Jwt = s_jwt
			});

		// create random unrelated boards
		foreach (CreateLeaderboardRequest req in _createBoardReqFaker.Generate(2))
		{
			await s_apiClient.Post<LeaderboardViewModel>(
				"/api/leaderboards",
				new()
				{
					Body = req,
					Jwt = s_jwt
				});
		}

		LeaderboardViewModel leaderboard = await s_apiClient.Get<LeaderboardViewModel>(
			$"api/leaderboards/{createReqBody.Slug}",
			new());

		leaderboard.Should().BeEquivalentTo(createdLeaderboard);
	}

	[Test]
	public async Task GetLeaderboards_BySlug_NotFound()
	{
		// populate with unrelated boards
		foreach (CreateLeaderboardRequest req in _createBoardReqFaker.Generate(2))
		{
			await s_apiClient.Post<LeaderboardViewModel>(
				"/api/leaderboards",
				new()
				{
					Body = req,
					Jwt = s_jwt
				});
		}

		CreateLeaderboardRequest reqForInexistentBoard = _createBoardReqFaker.Generate();

		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(() =>
			s_apiClient.Get<string>($"/api/leaderboards/{reqForInexistentBoard.Slug}", new()))!;

		e.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	private static string ListToQueryString<T>(IEnumerable<T> list, string key)
	{
		IEnumerable<string> queryList = list.Select(l => $"{key}={l}");
		return string.Join("&", queryList);
	}
}
