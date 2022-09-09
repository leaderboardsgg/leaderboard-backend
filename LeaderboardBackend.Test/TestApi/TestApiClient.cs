using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;

namespace LeaderboardBackend.Test.TestApi;

internal record HttpRequestInit
{
	public object? Body { get; init; }
	public string? Jwt { get; init; }
	public HttpMethod Method { get; init; } = HttpMethod.Get;
}

internal sealed class RequestFailureException : Exception
{
	public HttpResponseMessage Response { get; private set; }

	public RequestFailureException(HttpResponseMessage response) : base($"The attempted request failed with status code {response.StatusCode}")
	{
		Response = response;
	}
}

internal class TestApiClient
{
	private readonly HttpClient _client;

	public TestApiClient(HttpClient client)
	{
		_client = client;
	}

	public async Task<TResponse> Get<TResponse>(string endpoint, HttpRequestInit init)
	{
		return await SendAndRead<TResponse>(endpoint, init with { Method = HttpMethod.Get });
	}

	public async Task<TResponse> Post<TResponse>(string endpoint, HttpRequestInit init)
	{
		return await SendAndRead<TResponse>(endpoint, init with { Method = HttpMethod.Post });
	}

	public async Task<HttpResponseMessage> Delete(string endpoint, HttpRequestInit init)
	{
		return await Send(endpoint, init with { Method = HttpMethod.Delete });
	}

	private async Task<TResponse> SendAndRead<TResponse>(string endpoint, HttpRequestInit init)
	{
		HttpResponseMessage response = await Send(endpoint, init);

		return await ReadFromResponseBody<TResponse>(response);
	}

	private async Task<HttpResponseMessage> Send(string endpoint, HttpRequestInit init)
	{
		HttpResponseMessage response = await _client.SendAsync(
			CreateRequestMessage(
				endpoint,
				init,
				TestInitCommonFields.JsonSerializerOptions));

		if (!response.IsSuccessStatusCode)
		{
			throw new RequestFailureException(response);
		}

		return response;
	}

	private static async Task<T> ReadFromResponseBody<T>(HttpResponseMessage response)
	{
		string rawJson = await response.Content.ReadAsStringAsync();
		T? obj = JsonSerializer.Deserialize<T>(
			rawJson,
			TestInitCommonFields.JsonSerializerOptions);

		Assert.NotNull(obj);

		return obj!;
	}

	private static HttpRequestMessage CreateRequestMessage(
		string endpoint,
		HttpRequestInit init,
		JsonSerializerOptions options)
	{
		return new(init.Method, endpoint)
		{
			Headers =
			{
				Authorization = new(JwtBearerDefaults.AuthenticationScheme, init.Jwt)
			},
			Content = init.Body switch
			{
				not null => new StringContent(JsonSerializer.Serialize(init.Body, options))
				{
					Headers =
					{
						ContentType = new("application/json")
					}
				},
				_ => default
			}
		};
	}
}
