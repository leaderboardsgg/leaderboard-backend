using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class UserService : IUserService
{
	private readonly ApplicationContext ApplicationContext;

	public UserService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<User?> GetUserById(Guid id)
	{
		return await ApplicationContext.Users.FindAsync(id);
	}

	public async Task<User?> GetUserByEmail(string email)
	{
		return await ApplicationContext.Users.FirstOrDefaultAsync(u => u.Email == email);
	}

	public async Task<User?> GetUserByName(string name)
	{
		// We save a username with casing, but match without.
		// Effectively you can't have two separate users named e.g. "cool" and "cOoL".
		return await ApplicationContext.Users.FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == name.ToLower());
	}

	public async Task<User?> GetUserFromClaims(ClaimsPrincipal claims)
	{
		if (!claims.HasClaim(c => c.Type == JwtRegisteredClaimNames.Email))
		{
			return null;
		}

		string email = claims.FindFirstValue("Email");
		return await GetUserByEmail(email);
	}

	public async Task CreateUser(User user)
	{
		ApplicationContext.Users.Add(user);
		await ApplicationContext.SaveChangesAsync();
	}
}
