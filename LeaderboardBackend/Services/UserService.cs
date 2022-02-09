using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services
{
	public class UserService : IUserService
	{
		private ApplicationContext _applicationContext;
		private IConfiguration _config;
		public UserService(ApplicationContext applicationContext, IConfiguration config)
		{
			_applicationContext = applicationContext;
			_config = config;
		}

		public async Task<User?> GetUser(Guid id)
		{
			User? user = await _applicationContext.Users.FindAsync(id);
			return user;
		}

		public async Task<User?> GetUserByEmail(string email)
		{
			User? user = await _applicationContext.Users.SingleAsync(u => u.Email == email);
			return user;
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
}
