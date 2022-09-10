using System;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NodaTime;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Judgements
{
	private static TestApiClient s_ApiClient = null!;
	private static Leaderboard s_DefaultLeaderboard = null!;
	private static TestApiFactory s_Factory = null!;
	private static string? s_Jwt;

	private const string VALID_USERNAME = "Test";
	private const string VALID_PASSWORD = "c00l_pAssword";
	private const string VALID_EMAIL = "test@email.com";

	[SetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();

		// Set up a default Leaderboard and a mod user for that leaderboard to use as the Jwt for
		// tests
		string adminJwt = (await s_ApiClient.LoginAdminUser()).Token;
		User mod = await s_ApiClient.RegisterUser(VALID_USERNAME, VALID_EMAIL, VALID_PASSWORD);

		s_DefaultLeaderboard = await s_ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString()
				},
				Jwt = adminJwt
			});

		Modship modship = await s_ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = s_DefaultLeaderboard.Id,
					UserId = mod.Id
				},
				Jwt = adminJwt
			});

		s_Jwt = (await s_ApiClient.LoginUser(VALID_EMAIL, VALID_PASSWORD)).Token;
	}

	[Test]
	public async Task CreateJudgement_OK()
	{
		Run run = await CreateRun();

		JudgementViewModel? createdJudgement = await s_ApiClient.Post<JudgementViewModel>(
			"/api/judgements",
			new()
			{
				Body = new CreateJudgementRequest
				{
					RunId = run.Id,
					Note = "It is a cool run",
					Approved = true
				},
				Jwt = s_Jwt
			});

		Assert.NotNull(createdJudgement);
	}

	private static async Task<Run> CreateRun()
	{
		return await s_ApiClient.Post<Run>(
			"/api/runs",
			new()
			{
				Body = new CreateRunRequest
				{
					PlayedOn = LocalDate.MinIsoValue,
					SubmittedAt = Instant.MaxValue,
					Status = RunStatus.Submitted
				},
				Jwt = s_Jwt
			});
	}
}
