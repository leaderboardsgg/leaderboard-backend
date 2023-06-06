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
            "/api/users/register",
            new()
            {
                Body = new RegisterRequest()
                {
                    Username = username,
                    Password = password,
                    PasswordConfirm = password,
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
            "/api/users/login",
            new()
            {
                Body = new LoginRequest() { Email = email, Password = password, }
            }
        );
    }

    public static async Task<LoginResponse> LoginAdminUser(this TestApiClient apiClient)
    {
        return await apiClient.Post<LoginResponse>(
            "/api/users/login",
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
}
