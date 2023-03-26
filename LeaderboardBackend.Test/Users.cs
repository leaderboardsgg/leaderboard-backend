using System;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Users
{
	private static TestApiFactory s_factory = null!;
	private static TestApiClient s_apiClient = null!;

	private const string VALID_USERNAME = "Test64";
	private const string VALID_PASSWORD = "c00l_pAssword";
	private const string VALID_EMAIL = "test@email.com";

	[OneTimeSetUp]
	public void OneTimeSetUp()
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
	public void SetUp()
	{
		s_factory.ResetDatabase();
	}

	[Test]
	public static void GetUser_NotFound()
	{
		Guid randomGuid = new();
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_apiClient.Get<User>($"/api/users/{randomGuid}", new()))!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task GetUser_OK()
	{
		User createdUser = await s_apiClient.RegisterUser(
			VALID_USERNAME,
			VALID_EMAIL,
			VALID_PASSWORD);

		User retrievedUser = await s_apiClient.Get<User>($"/api/users/{createdUser?.Id}", new());

		Assert.AreEqual(createdUser, retrievedUser);
	}

	[Test]
	public static void Register_BadRequest()
	{
		RegisterRequest[] requests =
		{
			new(),
			new()
			{
				Username = VALID_USERNAME,
				Password = VALID_PASSWORD,
				PasswordConfirm = "someotherpassword",
				Email = VALID_EMAIL
			},
			new()
			{
				Username = VALID_USERNAME,
				Password = VALID_PASSWORD,
				PasswordConfirm = VALID_PASSWORD,
				Email = "whatisthis"
			},
			new()
			{
				Username = "B",
				Password = VALID_PASSWORD,
				PasswordConfirm = VALID_PASSWORD,
				Email = VALID_EMAIL
			}
		};

		foreach (RegisterRequest request in requests)
		{
			// Not using the helper here because it's easier for this test implementation
			// to have a table of requests and send them directly.
			RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
				await s_apiClient.Post<User>("/api/users/register", new() { Body = request }))!;

			Assert.AreEqual(
				HttpStatusCode.BadRequest,
				e.Response.StatusCode,
				$"{request} did not produce BadRequest, produced {e.Response.StatusCode}");
		}
	}

	[Test]
	public static void Me_Unauthorized()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_apiClient.Get<User>($"/api/users/me", new()))!;

		Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
	}

	[Test]
	public static async Task FullAuthFlow()
	{
		// Register User
		User createdUser = await s_apiClient.RegisterUser(
			VALID_USERNAME,
			VALID_EMAIL,
			VALID_PASSWORD);

		// Login
		LoginResponse login = await s_apiClient.LoginUser(createdUser.Email, VALID_PASSWORD);

		// Me
		User me = await s_apiClient.Get<User>("api/users/me", new() { Jwt = login.Token });

		Assert.AreEqual(createdUser, me);
	}
}
