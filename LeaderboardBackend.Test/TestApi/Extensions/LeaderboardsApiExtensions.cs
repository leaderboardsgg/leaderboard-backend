using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

public static class LeaderboardsApiExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> GetLeaderboard(
            long id) => client.GetAsync($"/api/leaderboards/{id}");

        public Task<HttpResponseMessage> GetLeaderboardBySlug(
            string slug) =>
        client.GetAsync($"/api/leaderboards/{slug}");

        public Task<HttpResponseMessage> GetLeaderboards(
            int? limit = null,
            int? offset = null,
            StatusFilter? status = null,
            SortLeaderboardsBy? sortBy = null
        ) => client.GetAsync($"/api/leaderboards{QueryParams.Format(
            ("limit", limit),
            ("offset", offset),
            ("status", status),
            ("sortBy", sortBy))}");

        public Task<HttpResponseMessage> SearchLeaderboards(
            string query,
            int? limit = null,
            int? offset = null,
            StatusFilter? status = null
        ) => client.GetAsync($"/api/search/leaderboards{QueryParams.Format(
                ("q", query),
                ("limit", limit),
                ("offset", offset),
                ("status", status))}");

        public Task<HttpResponseMessage> CreateLeaderboard(
            CreateLeaderboardRequest request
        ) => client.PostAsJsonAsync(
            "/leaderboards",
            request, TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> UpdateLeaderboard(
            long id,
            UpdateLeaderboardRequest request
        ) => client.PatchAsJsonAsync(
            $"/leaderboards/{id}",
            request,
            TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> DeleteLeaderboard(
            long id
        ) => client.DeleteAsync($"/leaderboards/{id}");
    }
}
