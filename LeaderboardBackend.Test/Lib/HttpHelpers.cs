using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class HttpHelpers
{
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
