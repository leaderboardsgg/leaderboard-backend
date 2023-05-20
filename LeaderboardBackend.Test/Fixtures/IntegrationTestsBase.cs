using System.Net.Http;
using LeaderboardBackend.Test.TestApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Fixtures;
public abstract class IntegrationTestsBase
{
    protected HttpClient Client { get; private set; } = null!;

    protected readonly static TestApiFactory s_factory = new();

    [SetUp]
    public void SetUp()
    {
        Client = s_factory.CreateClient();
    }

    protected void SetClientBearer(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new(JwtBearerDefaults.AuthenticationScheme, token);
    }
}
