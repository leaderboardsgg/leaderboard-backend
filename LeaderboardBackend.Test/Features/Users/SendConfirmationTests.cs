using System;
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
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

public class SendConfirmationTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;
    private IAuthService _authService = null!;

    [SetUp]
    public void Init()
    {
        _scope = _factory.Services.CreateScope();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _factory.ResetDatabase();
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
            Email = "test@email.com",
            Password = "password",
            Username = "username",
            Role = UserRole.Confirmed,
        };
        context.Add(user);
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
        ).Throws(new Exception());
        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
            });
        })
        .CreateClient();

        IUserService userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
        CreateUserResult result = await userService.CreateUser(new()
        {
            Email = "test@email.com",
            Password = "password",
            Username = "username",
        });
        string token = _authService.GenerateJSONWebToken(result.AsT0);

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await client.PostAsync(Routes.RESEND_CONFIRMATION, null);
        res.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task ResendConfirmation_Success()
    {
        Mock<IEmailSender> emailSenderMock = new();
        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
                services.AddSingleton<IClock, FakeClock>(_ => new(Instant.FromUnixTimeSeconds(1)));
            });
        })
        .CreateClient();
        IUserService userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
        CreateUserResult result = await userService.CreateUser(new()
        {
            Email = "test@email.com",
            Password = "password",
            Username = "username",
        });
        string token = _authService.GenerateJSONWebToken(result.AsT0);

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {token}");
        HttpResponseMessage res = await client.PostAsync(Routes.RESEND_CONFIRMATION, null);
        res.Should().HaveStatusCode(HttpStatusCode.OK);
        emailSenderMock.Verify(x =>
            x.EnqueueEmailAsync(
                "test@email.com",
                "Confirm Your Account",
                It.IsAny<string>()
            ),
            Times.Once()
        );

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        AccountConfirmation confirmation = context.AccountConfirmations.First(c => c.UserId == result.AsT0.Id);
        confirmation.Should().NotBeNull();
        confirmation.CreatedAt.ToUnixTimeSeconds().Should().Be(1);
        confirmation.UsedAt.Should().BeNull();
        Instant.Subtract(confirmation.ExpiresAt, confirmation.CreatedAt).Should().Be(Duration.FromHours(1));
    }
}
