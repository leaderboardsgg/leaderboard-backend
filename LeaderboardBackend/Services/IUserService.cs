using LeaderboardBackend.Models.Entities;
using System.Security.Claims;

namespace LeaderboardBackend.Services;

public interface IUserService
{
	Task<User?> GetUserById(Guid id);
	Task<User?> GetUserByEmail(string email);
	Task<User?> GetUserByName(string name);
	Task<User?> GetUserFromClaims(ClaimsPrincipal claims);
	Task CreateUser(User user);
}
