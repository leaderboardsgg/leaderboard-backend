using System.Net.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Fixtures;

public abstract class IntegrationTestsBase
{
    protected HttpClient _client = null!;

    protected WebApplicationFactory<Program> _factory = null!;

    protected void SetClientBearer(string token) => _client.DefaultRequestHeaders.Authorization = new(JwtBearerDefaults.AuthenticationScheme, token);

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public void ClearToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
