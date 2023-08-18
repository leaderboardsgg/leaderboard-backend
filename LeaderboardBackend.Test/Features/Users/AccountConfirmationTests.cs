using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class AccountConfirmationTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;
    private IAuthService _authService = null!;

    [SetUp]
    public void Init()
    {
        _scope = s_factory.Services.CreateScope();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
    }

    [TearDown]
    public void TearDown()
    {
        s_factory.ResetDatabase();
        _scope.Dispose();
    }

    [Test]
    public async Task ResendConfirmation_Unauthorised()
    {

        HttpResponseMessage res = await Client.PostAsync(Routes.RESEND_CONFIRMATION, null);
        res.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ResendConfirmation_NotFound_ShouldGet401()
    {
        string token = _authService.GenerateJSONWebToken(new()
        {
            Email = "unknown@user.com",
            Password = "password",
            Username = "username",
        });

        Client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await Client.PostAsync(Routes.RESEND_CONFIRMATION, null);

        res.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
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
        HttpResponseMessage res = await Client.PostAsync(Routes.RESEND_CONFIRMATION, null);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ResendConfirmation_EmailFailedToSend()
    {
        Mock<IEmailSender> emailSenderMock = new();
        emailSenderMock.Setup(e =>
            e.EnqueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
        ).Throws(new System.Exception());
        HttpClient client = s_factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmailSender>(_ => emailSenderMock.Object);
            });
        })
        .CreateClient();

        IUserService userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
        CreateUserResult result = await userService.CreateUser(new()
        {
            Email = "email",
            Password = "password",
            Username = "username",
        });
        string token = _authService.GenerateJSONWebToken(result.AsT0);

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await client.PostAsync(Routes.RESEND_CONFIRMATION, null);
        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ResendConfirmation_Success()
    {
        Mock<IEmailSender> emailSenderMock = new();
        HttpClient client = s_factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmailSender>(_ => emailSenderMock.Object);
            });
        })
        .CreateClient();
        IUserService userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
        CreateUserResult result = await userService.CreateUser(new()
        {
            Email = "email",
            Password = "password",
            Username = "username",
        });
        string token = _authService.GenerateJSONWebToken(result.AsT0);

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await client.PostAsync(Routes.RESEND_CONFIRMATION, null);
        res.Should().HaveStatusCode(HttpStatusCode.OK);
        emailSenderMock.Verify(x =>
            x.EnqueueEmailAsync(
                "email",
                "Confirmation",
                It.IsAny<string>()
            ),
            Times.Once()
        );
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        context.AccountConfirmations.FirstOrDefault(c => c.UserId == result.AsT0.Id)
            .Should().NotBeNull();
    }
}
