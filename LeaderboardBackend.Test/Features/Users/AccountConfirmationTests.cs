using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class AccountConfirmationTests : IntegrationTestsBase
{
    private const string RESEND_CONFIRMATION_URI = "/account/confirm";
    private IServiceScope _scope = null!;
    private IAuthService _authService = null!;
    private IUserService _userService = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _scope = s_factory.Services.CreateScope();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _scope.Dispose();
    }

    [Test]
    public async Task ResendConfirmation_Unauthorised()
    {

        HttpResponseMessage res = await Client.PostAsync(RESEND_CONFIRMATION_URI, null);
        res.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ResendConfirmation_NotFound()
    {
        string token = _authService.GenerateJSONWebToken(new()
        {
            Email = "unknown@user.com",
            Password = "password",
            Username = "username",
        });

        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await Client.PostAsync(RESEND_CONFIRMATION_URI, null);

        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ResendConfirmation_Conflict()
    {
        // TODO: Call UserService instead, once we're able to set a user's role with it.
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        User user = new()
        {
            Email = "email",
            Password = "password",
            Username = "username",
            Role = UserRole.Confirmed,
        };
        context.Add<User>(user);
        context.SaveChanges();
        string token = _authService.GenerateJSONWebToken(user);

        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await Client.PostAsync(RESEND_CONFIRMATION_URI, null);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ResendConfirmation_Success()
    {
        CreateUserResult result = await _userService.CreateUser(new()
        {
            Email = "email1",
            Password = "password",
            Username = "username1",
        });
        string token = _authService.GenerateJSONWebToken(result.AsT0);

        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await Client.PostAsync(RESEND_CONFIRMATION_URI, null);
        res.Should().HaveStatusCode(HttpStatusCode.OK);
    }
}
