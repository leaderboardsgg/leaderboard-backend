using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class HttpHelpers
{
	/// <typeparam name="Body">Object class that defines the POST's request body.</typeparam>
	/// <typeparam name="Res">Response object class from the POST.</typeparam>
	public static async Task<Res> Post<Body, Res>(string endpoint, Body requestBody, HttpClient httpClient, JsonSerializerOptions options)
	{
		HttpResponseMessage response = await httpClient.PostAsJsonAsync(endpoint, requestBody, options);
		response.EnsureSuccessStatusCode();
		return await HttpHelpers.ReadFromResponseBody<Res>(response, options);
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
}
