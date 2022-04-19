using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Users;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class UserHelpers
{
	public static async Task<User> Register(
		HttpClient apiClient,
		string username,
		string email,
		string password
	) =>
		await HttpHelpers.Post<User>(
			apiClient,
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

	public static async Task<LoginResponse> Login(
		HttpClient apiClient,
		string email,
		string password
	) =>
		await HttpHelpers.Post<LoginResponse>(
			apiClient,
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

	public static async Task<LoginResponse> LoginAdmin(
		HttpClient apiClient
	) =>
		await HttpHelpers.Post<LoginResponse>(
			apiClient,
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
