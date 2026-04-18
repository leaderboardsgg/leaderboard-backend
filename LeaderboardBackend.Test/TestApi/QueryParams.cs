using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaderboardBackend.Test.TestApi;

public record QueryParam(string Key, object? Value);

public static class QueryParamExtensions
{
    extension(IEnumerable<QueryParam> queryParams)
    {
        public string ToUrlString() => new UriBuilder
        {
            Query = string.Join('&', from pair in queryParams
                                     where pair.Value != null
                                     select $"{pair.Key}={pair.Value}")
        }.Query;
    }
}
