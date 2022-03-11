using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LeaderboardBackend.Services;

public class UserService : IUserService
{
	private readonly ApplicationContext _applicationContext;

	public UserService(ApplicationContext applicationContext, IConfiguration config)
	{
		_applicationContext = applicationContext;
	}

	public async Task<User?> GetUser(Guid id)
	{
		return await _applicationContext.Users.FindAsync(id);
	}

	public async Task<User?> GetUserByEmail(string email)
	{
		return await _applicationContext.Users.FirstOrDefaultAsync(u => u.Email == email);
	}

	public async Task<User?> GetUserByName(string name)
	{
		// We save a username with casing, but match without.
		// Effectively you can't have two separate users named e.g. "cool" and "cOoL".
		return await _applicationContext.Users.SingleOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == name.ToLower());
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
		_applicationContext.Users.Add(user);
		await _applicationContext.SaveChangesAsync();
	}
}
