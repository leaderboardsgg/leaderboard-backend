using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

public static class CategoriesApiExtensions
{
    public static Task<HttpResponseMessage> GetCategory(
        this HttpClient client,
        long id) => client.GetAsync($"/api/categories/{id}");

    public static Task<HttpResponseMessage> GetCategory(
        this HttpClient client,
        long lbId,
        string slug) =>
    client.GetAsync($"/api/leaderboards/{lbId}/categories/{slug}");

    public static Task<HttpResponseMessage> GetCategoriesForLeaderboard(
        this HttpClient client,
        long lbId,
        int? limit = null,
        int? offset = null,
        StatusFilter? filter = null)
    {
        Dictionary<string, object?> qParams = new()
        {
            { "limit", limit },
            { "offset", offset },
            { "status", filter}
        };

        UriBuilder uriBuilder = new()
        {
            Query = string.Join('&', qParams.SelectMany<KeyValuePair<string, object?>, string>(
                    pair => pair.Value is null ? [] : [$"{pair.Key}={pair.Value}"]))
        };

        return client.GetAsync($"/api/leaderboards/{lbId}/categories" + uriBuilder.Query);
    }

    public static Task<HttpResponseMessage> CreateCategory(
        this HttpClient client,
        long lbId,
        CreateCategoryRequest request
    ) => client.PostAsJsonAsync(
        $"/leaderboards/{lbId}/categories",
        request, TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> UpdateCategory(
        this HttpClient client,
        long id,
        UpdateCategoryRequest request
    ) => client.PatchAsJsonAsync(
        $"/categories/{id}",
        request,
        TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> DeleteCategory(
        this HttpClient client,
        long id
    ) => client.DeleteAsync($"/categories/{id}");
}
