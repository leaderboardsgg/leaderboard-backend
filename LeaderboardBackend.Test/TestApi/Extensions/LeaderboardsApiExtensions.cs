using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

public static class LeaderboardsApiExtensions
{
    public static Task<HttpResponseMessage> GetLeaderboard(
        this HttpClient client,
        long id) => client.GetAsync($"/api/leaderboards/{id}");

    public static Task<HttpResponseMessage> GetLeaderboardBySlug(
        this HttpClient client,
        string slug) =>
    client.GetAsync($"/api/leaderboards/{slug}");

    public static Task<HttpResponseMessage> GetLeaderboards(
        this HttpClient client,
        Page? page,
        StatusFilter? status,
        SortLeaderboardsBy? sortBy
    )
    {
        Dictionary<string, object?> qParams = new()
        {
            { "limit", page?.Limit },
            { "offset", page?.Offset },
            { "status", status },
            { "sortBy", sortBy },
        };

        UriBuilder b = new()
        {
            Query = string.Join('&', from pair in qParams
                                     where pair.Value != null
                                     select $"{pair.Key}={pair.Value}")
        };

        return client.GetAsync($"/api/leaderboards{b.Query}");
    }

    public static Task<HttpResponseMessage> SearchLeaderboards(
        this HttpClient client,
        string query,
        Page page,
        StatusFilter? status
    )
    {
        Dictionary<string, object?> qParams = new()
        {
            { "page", page },
            { "status", status },
            { "query", query },
        };

        UriBuilder b = new()
        {
            Query = string.Join('&', from pair in qParams
                                     where pair.Value != null
                                     select $"{pair.Key}={pair.Value}")
        };

        return client.GetAsync($"/api/search/leaderboards{b.Query}");
    }

    public static Task<HttpResponseMessage> CreateLeaderboard(
        this HttpClient client,
        CreateLeaderboardRequest request
    ) => client.PostAsJsonAsync(
        "/leaderboards",
        request, TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> UpdateLeaderboard(
        this HttpClient client,
        long id,
        UpdateLeaderboardRequest request
    ) => client.PatchAsJsonAsync(
        $"/leaderboards/{id}",
        request,
        TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> DeleteLeaderboard(
        this HttpClient client,
        long id
    ) => client.DeleteAsync($"/leaderboards/{id}");
}
