using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test.TestApi.Extensions;

internal static class UserApiExtensions
{
    public static async Task<UserViewModel> RegisterUser(
        this TestApiClient client,
        string username,
        string email,
        string password
    )
    {
        return await client.Post<UserViewModel>(
            Routes.REGISTER,
            new()
            {
                Body = new RegisterRequest()
                {
                    Username = username,
                    Password = password,
                    Email = email,
                }
            }
        );
    }

    public static async Task<LoginResponse> LoginUser(
        this TestApiClient apiClient,
        string email,
        string password
    )
    {
        return await apiClient.Post<LoginResponse>(
            Routes.LOGIN,
            new()
            {
                Body = new LoginRequest() { Email = email, Password = password, }
            }
        );
    }

    public static async Task<LoginResponse> LoginAdminUser(this TestApiClient apiClient)
    {
        return await apiClient.Post<LoginResponse>(
            Routes.LOGIN,
            new()
            {
                Body = new LoginRequest()
                {
                    Email = TestInitCommonFields.Admin.Email,
                    Password = TestInitCommonFields.Admin.Password,
                }
            }
        );
    }

    public static Task<HttpResponseMessage> RegisterUser(
        this HttpClient client,
        string username,
        string email,
        string password
    ) => client.PostAsJsonAsync(Routes.REGISTER, new RegisterRequest()
        {
            Username = username,
            Password = password,
            Email = email,
        }, TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> LoginUser(
        this HttpClient client,
        string email,
        string password
    ) => client.PostAsJsonAsync(Routes.LOGIN, new LoginRequest()
        {
            Email = email,
            Password = password
        }, TestInitCommonFields.JsonSerializerOptions);

    public static Task<HttpResponseMessage> LoginAdminUser(
        this HttpClient client) => client.PostAsJsonAsync(
            Routes.LOGIN,
            new LoginRequest()
            {
                Email = TestInitCommonFields.Admin.Email,
                Password = TestInitCommonFields.Admin.Password,
            },
            TestInitCommonFields.JsonSerializerOptions
        );
}
