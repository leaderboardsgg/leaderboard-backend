using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class UserService : IUserService
{
	private readonly ApplicationContext _applicationContext;

	public UserService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<User?> GetUserById(Guid id)
	{
		return await _applicationContext.Users
			.FindAsync(id);
	}

	public async Task<User?> GetUserByEmail(string email)
	{
		return await _applicationContext.Users
			.FirstOrDefaultAsync(u => u.Email == email);
	}

	public async Task<User?> GetUserByName(string name)
	{
		// We save a username with casing, but match without.
		// Effectively you can't have two separate users named e.g. "cool" and "cOoL".
		return await _applicationContext.Users
			.FirstOrDefaultAsync(u =>
				u.Username != null
				&& u.Username.ToLower() == name.ToLower());
	}

	public async Task CreateUser(User user)
	{
		_applicationContext.Users.Add(user);

		await _applicationContext.SaveChangesAsync();
	}
}
