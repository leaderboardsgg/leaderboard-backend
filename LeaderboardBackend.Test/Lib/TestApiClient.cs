using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal sealed class RequestFailureException : Exception
{
	public HttpResponseMessage Response { get; private set; }

	public RequestFailureException(HttpResponseMessage response) : base("The attempted request failed")
	{ 
		Response = response; 
	}
}

internal class TestApiClient
{
	private HttpClient Client;

	public TestApiClient(HttpClient client)
	{
		Client = client;
	}

	public async Task<Res> Get<Res>(
		string endpoint,
		HttpRequestInit init
	) => await Send<Res>(endpoint, init with { Method = HttpMethod.Get });

	public async Task<Res> Post<Res>(
		string endpoint,
		HttpRequestInit init
	) => await Send<Res>(endpoint, init with { Method = HttpMethod.Post });

	private async Task<Res> Send<Res>(
		string endpoint,
		HttpRequestInit init
	)
	{
		HttpResponseMessage response = await Client.SendAsync(
			CreateRequestMessage(
				endpoint,
				init,
				TestInitCommonFields.JsonSerializerOptions
			)
		);
		if (!response.IsSuccessStatusCode)
		{
			throw new RequestFailureException(response);
		}
		return await ReadFromResponseBody<Res>(response);
	}

	private async Task<T> ReadFromResponseBody<T>(HttpResponseMessage response)
	{
		string rawJson = await response.Content.ReadAsStringAsync();
		T? obj = JsonSerializer.Deserialize<T>(rawJson, TestInitCommonFields.JsonSerializerOptions);
		Assert.NotNull(obj);
		return obj!;
	}

	private HttpRequestMessage CreateRequestMessage(
		string endpoint,
		HttpRequestInit init,
		JsonSerializerOptions options
	) =>
		new(init.Method, endpoint)
		{
			Headers =
			{
				Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, init.Jwt)
			},
			Content = init.Body switch
			{
				not null => new StringContent(JsonSerializer.Serialize(init.Body, options))
				{
					Headers =
					{
						ContentType = new("application/json"),
					},
				},
				_ => default
			}
		};
}
