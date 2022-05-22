using System;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Judgements
{
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;
	private static Leaderboard DefaultLeaderboard = null!;
	private static string? Jwt;

	private static readonly string ValidUsername = "Test";
	private static readonly string ValidPassword = "c00l_pAssword";
	private static readonly string ValidEmail = "test@email.com";

	[SetUp]
	public static async Task SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();

		// Set up a default Leaderboard and a mod user for that leaderboard to use as the Jwt for tests
		string adminJwt = (await ApiClient.LoginAdminUser()).Token;
		User mod = await ApiClient.RegisterUser(
			ValidUsername,
			ValidEmail,
			ValidPassword
		);
		DefaultLeaderboard = await ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
				},
				Jwt = adminJwt,
			}
		);
		Modship modship = await ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = DefaultLeaderboard.Id,
					UserId = mod.Id,
				},
				Jwt = adminJwt,
			}
		);

		Jwt = (await ApiClient.LoginUser(ValidEmail, ValidPassword)).Token;
	}

	[Test]
	public async Task CreateJudgement_OK()
	{
		Run run = await CreateRun();

		JudgementViewModel? createdJudgement = await ApiClient.Post<JudgementViewModel>(
			"/api/judgements",
			new()
			{
				Body = new CreateJudgementRequest
				{
					RunId = run.Id,
					Note = "It is a cool run",
					Approved = true,
				},
				Jwt = Jwt,
			}
		);

		Assert.NotNull(createdJudgement);
	}

	private async Task<Run> CreateRun()
	{
		return await ApiClient.Post<Run>(
			"/api/runs",
			new()
			{
				Body = new CreateRunRequest
				{
					Played = DateTime.UtcNow,
					Submitted = DateTime.UtcNow,
					Status = RunStatus.SUBMITTED,
				},
				Jwt = Jwt,
			}
		);
	}
}
