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
	private static TestApiClient s_apiClient = null!;
	private static Leaderboard s_defaultLeaderboard = null!;
	private static TestApiFactory s_factory = null!;
	private static string? s_jwt;
	private static long s_categoryId;

	private const string VALID_USERNAME = "Test";
	private const string VALID_PASSWORD = "c00l_pAssword";
	private const string VALID_EMAIL = "test@email.com";

	[OneTimeSetUp]
	public void OneTimeSetup()
	{
		s_factory = new TestApiFactory();
		s_apiClient = s_factory.CreateTestApiClient();
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		s_factory.Dispose();
	}

	[SetUp]
	public async Task SetUp()
	{
		s_factory.ResetDatabase();

		// Set up a default Leaderboard and a mod user for that leaderboard to use as the Jwt for
		// tests
		string adminJwt = (await s_apiClient.LoginAdminUser()).Token;
		User mod = await s_apiClient.RegisterUser(VALID_USERNAME, VALID_EMAIL, VALID_PASSWORD);

		s_defaultLeaderboard = await s_apiClient.Post<Leaderboard>(
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

		Category createdCategory = await s_apiClient.Post<Category>(
				"/api/categories",
				new()
				{
					Body = new CreateCategoryRequest()
					{
						Name = Generators.GenerateRandomString(),
						Slug = Generators.GenerateRandomString(),
						LeaderboardId = s_defaultLeaderboard.Id,
					},
					Jwt = adminJwt,
				}
			);
		s_categoryId = createdCategory.Id;

		Modship modship = await s_apiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = s_defaultLeaderboard.Id,
					UserId = mod.Id
				},
				Jwt = adminJwt
			});

		s_jwt = (await s_apiClient.LoginUser(VALID_EMAIL, VALID_PASSWORD)).Token;
	}

	[Test]
	public async Task CreateJudgement_OK()
	{
		Run run = await CreateRun();

		JudgementViewModel? createdJudgement = await s_apiClient.Post<JudgementViewModel>(
			"/api/judgements",
			new()
			{
				Body = new CreateJudgementRequest
				{
					RunId = run.Id,
					Note = "It is a cool run",
					Approved = true
				},
				Jwt = s_jwt
			});

		Assert.NotNull(createdJudgement);
	}

	private static async Task<Run> CreateRun()
	{
		return await s_apiClient.Post<Run>(
			"/api/runs",
			new()
			{
				Body = new CreateRunRequest
				{
					PlayedOn = LocalDate.MinIsoValue,
					SubmittedAt = Instant.MaxValue,
					Status = RunStatus.Submitted,
					CategoryId = s_categoryId
				},
				Jwt = s_jwt
			});
	}
}
