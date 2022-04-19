using NUnit.Framework;
using System.Net;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests.Users;
using LeaderboardBackend.Models.Entities;
using System.Net.Http.Json;
using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Users
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;

	private static readonly string ValidUsername = "Test";
	private static readonly string ValidPassword = "c00l_pAssword";
	private static readonly string ValidEmail = "test@email.com";

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
	}

	[Test]
	public static async Task GetUser_NotFound()
	{
		Guid randomGuid = new();
		HttpResponseMessage response = await ApiClient.GetAsync($"/api/users/{randomGuid}");
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task GetUser_Found()
	{
		User createdUser = await UserHelpers.Register(
			ApiClient,
			ValidUsername,
			ValidEmail,
			ValidPassword
		);

		User retrievedUser = await HttpHelpers.Get<User>(
			ApiClient,
			$"/api/users/{createdUser?.Id}",
			new()
		);

		Assert.AreEqual(createdUser, retrievedUser);
	}

	[Test]
	public static async Task Register_BadRequest()
	{
		RegisterRequest[] requests = {
			new() {},
			new()
			{
				Username = ValidUsername,
				Password = ValidPassword,
				PasswordConfirm = "someotherpassword",
				Email = ValidEmail,
			},
			new()
			{
				Username = ValidUsername,
				Password = ValidPassword,
				PasswordConfirm = ValidPassword,
				Email = "whatisthis",
			},
			new()
			{
				Username = "B",
				Password = ValidPassword,
				PasswordConfirm = ValidPassword,
				Email = ValidEmail,
			},
		};

		foreach (RegisterRequest request in requests)
		{
			HttpResponseMessage registerResponse = await ApiClient.PostAsJsonAsync("/api/users/register", request);
			Assert.AreEqual(
				HttpStatusCode.BadRequest,
				registerResponse.StatusCode,
				$"{request} did not produce BadRequest, produced {registerResponse.StatusCode}"
			);
		}
	}

	[Test]
	public static async Task Me_Unauthorized()
	{
		HttpRequestMessage meRequest = new(HttpMethod.Get, "/api/users/me");
		HttpResponseMessage meResponse = await ApiClient.SendAsync(meRequest);
		Assert.AreEqual(HttpStatusCode.Unauthorized, meResponse.StatusCode);
	}

	[Test]
	public static async Task FullAuthFlow()
	{
		// Register User
		RegisterRequest registerBody = new()
		{
			Username = ValidUsername,
			Password = ValidPassword,
			PasswordConfirm = ValidPassword,
			Email = ValidEmail,
		};
		HttpResponseMessage registerResponse = await ApiClient.PostAsJsonAsync("/api/users/register", registerBody);
		registerResponse.EnsureSuccessStatusCode();
		User createdUser = await HttpHelpers.ReadFromResponseBody<User>(registerResponse);

		// Login
		LoginRequest loginBody = new()
		{
			Email = createdUser.Email,
			Password = ValidPassword,
		};
		HttpResponseMessage loginResponse = await ApiClient.PostAsJsonAsync("/api/users/login", loginBody);
		loginResponse.EnsureSuccessStatusCode();

		string token = (await HttpHelpers.ReadFromResponseBody<LoginResponse>(loginResponse)).Token;

		// Me
		HttpRequestMessage meRequest = new(
			HttpMethod.Get,
			"/api/users/me")
		{
			Headers =
			{
				Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token)
			}
		};
		HttpResponseMessage meResponse = await ApiClient.SendAsync(meRequest);
		meResponse.EnsureSuccessStatusCode();
		User meUser = await HttpHelpers.ReadFromResponseBody<User>(registerResponse);
		Assert.AreEqual(createdUser, meUser);
	}
}
