using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Users
{
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;

	private static readonly string ValidUsername = "Test";
	private static readonly string ValidPassword = "c00l_pAssword";
	private static readonly string ValidEmail = "test@email.com";

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();
	}

	[Test]
	public static void GetUser_NotFound()
	{
		Guid randomGuid = new();
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await ApiClient.Get<User>(
				$"/api/users/{randomGuid}",
				new()
			)
		)!;
		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task GetUser_OK()
	{
		User createdUser = await ApiClient.RegisterUser(
			ValidUsername,
			ValidEmail,
			ValidPassword
		);

		User retrievedUser = await ApiClient.Get<User>(
			$"/api/users/{createdUser?.Id}",
			new()
		);

		Assert.AreEqual(createdUser, retrievedUser);
	}

	[Test]
	public static void Register_BadRequest()
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
			// Not using the helper here because it's easier for this test implementation
			// to have a table of requests and send them directly.
			RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
				await ApiClient.Post<User>("/api/users/register", new() { Body = request })
			)!;
			Assert.AreEqual(
				HttpStatusCode.BadRequest,
				e.Response.StatusCode,
				$"{request} did not produce BadRequest, produced {e.Response.StatusCode}"
			);
		}
	}

	[Test]
	public static void Me_Unauthorized()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await ApiClient.Get<User>(
				$"/api/users/me",
				new()
			)
		)!;
		Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
	}

	[Test]
	public static async Task FullAuthFlow()
	{
		// Register User
		User createdUser = await ApiClient.RegisterUser(
			ValidUsername,
			ValidEmail,
			ValidPassword
		);

		// Login
		LoginResponse login = await ApiClient.LoginUser(createdUser.Email, ValidPassword);

		// Me
		User me = await ApiClient.Get<User>(
			"api/users/me",
			new()
			{
				Jwt = login.Token
			}
		);
		Assert.AreEqual(createdUser, me);
	}
}
