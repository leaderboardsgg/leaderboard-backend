using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

internal static class UserApiExtensions
{
    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> RegisterUser(
            string username,
            string email,
            string password
        ) =>
        client.PostAsJsonAsync(Routes.REGISTER, new RegisterRequest()
        {
            Username = username,
            Password = password,
            Email = email,
        }, TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> LoginUser(
            string email,
            string password
        ) =>
        client.PostAsJsonAsync(Routes.LOGIN, new LoginRequest()
        {
            Email = email,
            Password = password
        }, TestInitCommonFields.JsonSerializerOptions);

        public Task<HttpResponseMessage> LoginAdminUser() =>
        client.PostAsJsonAsync(
            Routes.LOGIN,
            new LoginRequest()
            {
                Email = TestInitCommonFields.Admin.Email,
                Password = TestInitCommonFields.Admin.Password,
            },
            TestInitCommonFields.JsonSerializerOptions
        );
    }
}
