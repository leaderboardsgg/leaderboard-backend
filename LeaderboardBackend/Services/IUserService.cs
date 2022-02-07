using System.Security.Claims;
using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IUserService
	{
		Task<User?> GetUser(Guid id);

		Task<User?> GetUserByEmail(string email);

		Task<User?> GetUserFromClaims(ClaimsPrincipal claims);

		Task CreateUser(User user);
	}
}
