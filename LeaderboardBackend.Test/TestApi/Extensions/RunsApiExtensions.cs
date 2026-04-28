using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

public static class RunsApiExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> GetRun(Guid id) =>
            client.GetAsync($"/api/runs/{id.ToUrlSafeBase64String()}");

        public Task<HttpResponseMessage> CreateRun(
            long catId,
            CreateRunRequest request) => client.PostAsJsonAsync(
                $"/categories/{catId}/runs",
                request,
                TestInitCommonFields.JsonSerializerOptions);

        public async Task<HttpResponseMessage> GetRunsForCategory(
            long catId,
            int? limit = null,
            int? offset = null,
            StatusFilter? filter = null)
        {
            QueryParam[] qParams = [
                new("limit", limit),
                new("offset", offset),
                new("status", filter)];

            return await client.GetAsync($"/api/categories/{catId}/runs{qParams.ToUrlString()}");
        }

        public Task<HttpResponseMessage> GetRecordsForCategory(
            long catId,
            int? limit = null,
            int? offset = null)
        {
            QueryParam[] qParams = [
                new("limit", limit),
                new("offset", offset)];

            return client.GetAsync(
                $"/api/categories/{catId}/records" + qParams.ToUrlString());
        }

        public Task<HttpResponseMessage> GetCategoryForRun(Guid id) =>
            client.GetAsync($"/api/runs/{id.ToUrlSafeBase64String()}/category");

        public Task<HttpResponseMessage> UpdateRun(Guid id, UpdateRunRequest request) => client.PatchAsJsonAsync(
            $"/runs/{id.ToUrlSafeBase64String()}",
            request,
            TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> DeleteRun(Guid id) => client.DeleteAsync($"/runs/{id.ToUrlSafeBase64String()}");
    }
}
