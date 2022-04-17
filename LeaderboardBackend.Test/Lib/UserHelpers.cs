using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Users;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class UserHelpers
{
	public static async Task<User> Register(
		HttpClient apiClient,
		string username,
		string email,
		string password,
		JsonSerializerOptions options
	)
	{
		RegisterRequest registerBody = new()
		{
			Username = username,
			Password = password,
			PasswordConfirm = password,
			Email = email,
		};

		return await HttpHelpers.Send<User>(
			apiClient,
			"/api/users/register",
			new()
			{
				Method = HttpMethod.Post,
				Body = registerBody
			},
			options
		);
	}

	public static async Task<LoginResponse> Login(HttpClient apiClient, string email, string password, JsonSerializerOptions options)
	{
		LoginRequest loginBody = new()
		{
			Email = email,
			Password = password,
		};

		return await HttpHelpers.Send<LoginResponse>(
			apiClient,
			"/api/users/login",
			new()
			{
				Method = HttpMethod.Post,
				Body = loginBody
			},
			options
		);
	}
}
