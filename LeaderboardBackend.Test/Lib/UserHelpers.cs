using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Users;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Lib;

internal static class UserHelpers
{
	public static async Task<User> Register(HttpClient apiClient, UserTypes userType, JsonSerializerOptions jsonSerializerOptions)
	{
		RegisterRequest registerBody = new()
		{
			Username = "AyylmaoGaming",
			Password = "c00l_pAssword",
			PasswordConfirm = "c00l_pAssword",
			Email = "test@email.com",
		};

		return await HttpHelpers.Post<RegisterRequest, User>("/api/users/register", registerBody, apiClient, jsonSerializerOptions);
	}

	public static async Task<LoginResponse> Login(HttpClient apiClient, User user, JsonSerializerOptions jsonSerializerOptions)
	{
		LoginRequest loginBody = new()
		{
			Email = user.Email,
			Password = "c00l_pAssword",
		};

		return await HttpHelpers.Post<LoginRequest, LoginResponse>("/api/users/login", loginBody, apiClient, jsonSerializerOptions);
	}
}
