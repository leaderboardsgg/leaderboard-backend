using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class HttpHelpers
{
	/// <typeparam name="Body">Object class that defines the POST's request body.</typeparam>
	/// <typeparam name="Res">Response object class from the POST.</typeparam>
	public static async Task<Res> Get<Res>(
		string endpoint,
		HttpClient client,
		JsonSerializerOptions options,
		string? jwt = null
	)
	{
		return await Send<Res>(
			CreateRequestMessage(endpoint, HttpMethod.Get, options, jwt),
			client,
			options
		);
	}

	/// <typeparam name="Body">Object class that defines the POST's request body.</typeparam>
	/// <typeparam name="Res">Response object class from the POST.</typeparam>
	public static async Task<Res> Post<Body, Res>(
		string endpoint,
		Body body,
		HttpClient client,
		JsonSerializerOptions options,
		string? jwt = null
	)
	{
		return await Send<Res>(
			CreateRequestMessage(endpoint, HttpMethod.Post, options, jwt, body),
			client,
			options
		);
	}

	public static async Task<T> ReadFromResponseBody<T>(HttpResponseMessage response, JsonSerializerOptions jsonOptions)
	{
		string rawJson = await response.Content.ReadAsStringAsync();
		T? obj = JsonSerializer.Deserialize<T>(rawJson, jsonOptions);
		Assert.NotNull(obj);
		return obj!;
	}

	public static string ListToQueryString<T>(IEnumerable<T> list, string key)
	{
		IEnumerable<string> queryList = list.Select(l => $"{key}={l}");
		return string.Join("&", queryList);
	}

	private static HttpRequestMessage CreateRequestMessage(
		string endpoint,
		HttpMethod method,
		JsonSerializerOptions options,
		string? jwt,
		object? body = null
	)
	{
		HttpRequestMessage requestMessage = new(method, endpoint);

		if (body is not null)
		{
			StringContent content = new(JsonSerializer.Serialize(body, options));
			content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			requestMessage.Content = content;
		}

		if (jwt is not null)
		{
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt);
		}

		return requestMessage; // To be then called with HttpClient.SendAsync(requestMessage);
	}

	private static async Task<Res> Send<Res>(HttpRequestMessage message, HttpClient client, JsonSerializerOptions options)
	{
		HttpResponseMessage response = await client.SendAsync(message);
		response.EnsureSuccessStatusCode();
		return await HttpHelpers.ReadFromResponseBody<Res>(response, options);
	}
}
