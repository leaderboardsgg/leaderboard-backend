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
	public static async Task<Res> Get<Res>(
		HttpClient client,
		string endpoint,
		HttpRequestInit init
	) => await Send<Res>(client, endpoint, init with { Method = HttpMethod.Get });

	public static async Task<Res> Post<Res>(
		HttpClient client,
		string endpoint,
		HttpRequestInit init
	) => await Send<Res>(client, endpoint, init with { Method = HttpMethod.Post });

	public static async Task<T> ReadFromResponseBody<T>(HttpResponseMessage response)
	{
		string rawJson = await response.Content.ReadAsStringAsync();
		T? obj = JsonSerializer.Deserialize<T>(rawJson, TestInitCommonFields.JsonSerializerOptions);
		Assert.NotNull(obj);
		return obj!;
	}

	private static async Task<Res> Send<Res>(
		HttpClient client,
		string endpoint,
		HttpRequestInit init
	)
	{
		HttpResponseMessage response = await client.SendAsync(
			CreateRequestMessage(
				endpoint,
				init,
				TestInitCommonFields.JsonSerializerOptions
			)
		);
		response.EnsureSuccessStatusCode();
		return await HttpHelpers.ReadFromResponseBody<Res>(response);
	}

	private static HttpRequestMessage CreateRequestMessage(
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
