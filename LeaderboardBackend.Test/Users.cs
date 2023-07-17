using System;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
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
        RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(
            async () => await s_apiClient.Get<UserViewModel>($"/api/users/{randomGuid}", new())
        )!;

        Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
    }

    [Test]
    public static async Task GetUser_OK()
    {
        UserViewModel createdUser = await s_apiClient.RegisterUser(
            VALID_USERNAME,
            VALID_EMAIL,
            VALID_PASSWORD
        );

        UserViewModel retrievedUser = await s_apiClient.Get<UserViewModel>($"/api/users/{createdUser?.Id}", new());

        Assert.AreEqual(createdUser, retrievedUser);
    }

    [Test]
    public static void Me_Unauthorized()
    {
        RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(
            async () => await s_apiClient.Get<UserViewModel>($"/api/users/me", new())
        )!;

        Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
    }

    [Test]
    public static async Task FullAuthFlow()
    {
        // Register User
        UserViewModel createdUser = await s_apiClient.RegisterUser(
            VALID_USERNAME,
            VALID_EMAIL,
            VALID_PASSWORD
        );

        // Login
        LoginResponse login = await s_apiClient.LoginUser(VALID_EMAIL, VALID_PASSWORD);

        // Me
        UserViewModel me = await s_apiClient.Get<UserViewModel>("api/users/me", new() { Jwt = login.Token });

        createdUser.Should().BeEquivalentTo(me);
    }
}
