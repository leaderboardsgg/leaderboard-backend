using System;
using System.Linq;

namespace LeaderboardBackend.Test.TestApi;

public static class QueryParams
{
    public static string Format(params (string key, object? value)[] qParams) => new UriBuilder
    {
        Query = string.Join('&', from pair in qParams
                                 where pair.value != null
                                 select $"{pair.key}={pair.value}")
    }.Query;
}
