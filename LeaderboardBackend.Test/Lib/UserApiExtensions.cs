using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class UserApiExtensions
{
	public static async Task<User> RegisterUser(
		this TestApiClient client,
		string username,
		string email,
		string password
	) =>
		await client.Post<User>(
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

	public static async Task<LoginResponse> LoginUser(
		this TestApiClient apiClient,
		string email,
		string password
	) =>
		await apiClient.Post<LoginResponse>(
			"/api/users/login",
			new()
			{
				Body = new LoginRequest()
				{
					Email = email,
					Password = password,
				}
			}
		);

	public static async Task<LoginResponse> LoginAdminUser(
		this TestApiClient apiClient
	) =>
		await apiClient.Post<LoginResponse>(
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
