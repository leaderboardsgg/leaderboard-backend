using System.Security.Claims;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IUserService
{
	Task<User?> GetUserById(Guid id);
	Task<User?> GetUserByEmail(string email);
	Task<User?> GetUserByName(string name);
	Task<User?> GetUserFromClaims(ClaimsPrincipal claims);
	Task CreateUser(User user);
}
