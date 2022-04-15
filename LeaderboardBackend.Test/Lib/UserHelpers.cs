using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Users;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class UserHelpers
{
	public static async Task<User> Register(HttpClient apiClient, string username, string email, string password, JsonSerializerOptions jsonSerializerOptions)
	{
		RegisterRequest registerBody = new()
		{
			Username = username,
			Password = password,
			PasswordConfirm = password,
			Email = email,
		};

		return await HttpHelpers.Post<RegisterRequest, User>("/api/users/register", registerBody, apiClient, jsonSerializerOptions);
	}

	public static async Task<LoginResponse> Login(HttpClient apiClient, string email, string password, JsonSerializerOptions jsonSerializerOptions)
	{
		LoginRequest loginBody = new()
		{
			Email = email,
			Password = password,
		};

		return await HttpHelpers.Post<LoginRequest, LoginResponse>("/api/users/login", loginBody, apiClient, jsonSerializerOptions);
	}
}
