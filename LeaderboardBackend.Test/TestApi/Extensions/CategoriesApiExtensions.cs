using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

public static class CategoriesApiExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> GetCategory(
            long id) => client.GetAsync($"/api/categories/{id}");

        public Task<HttpResponseMessage> GetCategory(
            long lbId,
            string slug) =>
        client.GetAsync($"/api/leaderboards/{lbId}/categories/{slug}");

        public Task<HttpResponseMessage> GetCategoriesForLeaderboard(
            long lbId,
            int? limit = null,
            int? offset = null,
            StatusFilter? filter = null) => client.GetAsync(
                $"/api/leaderboards/{lbId}/categories{QueryParams.Format(
                    ("limit", limit),
                    ("offset", offset),
                    ("status", filter))}");

        public Task<HttpResponseMessage> CreateCategory(
            long lbId,
            CreateCategoryRequest request
        ) => client.PostAsJsonAsync(
            $"/leaderboards/{lbId}/categories",
            request, TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> UpdateCategory(
            long id,
            UpdateCategoryRequest request
        ) => client.PatchAsJsonAsync(
            $"/categories/{id}",
            request,
            TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> DeleteCategory(
            long id
        ) => client.DeleteAsync($"/categories/{id}");
    }
}
