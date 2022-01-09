using System.Security.Claims;
using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services
{
	public class UserService : IUserService
	{
		private UserContext _userContext;
		private IConfiguration _config;
		public UserService(UserContext userContext, IConfiguration config)
		{
			_userContext = userContext;
			_config = config;
		}

		public async Task<User?> GetUser(long id)
		{
			User? user = await _userContext.Users.FindAsync(id);
			return user;
		}

		public async Task<User?> GetUserByEmail(string email)
		{
			User? user = await _userContext.Users.SingleAsync(u => u.Email == email);
			return user;
		}

		public async Task<User?> GetUserFromClaims(ClaimsPrincipal claims)
		{
			if (!claims.HasClaim(c => c.Type == "Email"))
			{
				return null;
			}
			string email = claims.FindFirstValue("Email");
			return await GetUserByEmail(email);
		}

		public async Task CreateUser(User user)
		{
			_userContext.Users.Add(user);
			await _userContext.SaveChangesAsync();
		}
	}
}
