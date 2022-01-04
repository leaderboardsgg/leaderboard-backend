using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

        public async Task<User> GetUser(long id)
        {
            User? user = await _userContext.Users.FindAsync(id);
            if (user == null)
            {
                throw new BadHttpRequestException($"Cannot find user with id {id}");
            }
            return (User)user;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            User user = await _userContext.Users.SingleAsync(u => u.Email == email);
            if (user == null)
            {
                throw new BadHttpRequestException($"Cannot find user with email {email}");
            }
            return user;
        }

        public async Task<User> GetUserFromClaims(ClaimsPrincipal claims)
        {
            if (!claims.HasClaim(c => c.Type == "Email"))
            {
                throw new BadHttpRequestException("Email missing from claims");
            }
            string email = claims.FindFirstValue("Email");
            return await GetUserByEmail(email);
        }

        public async Task CreateUser(User user)
        {
            _userContext.Users.Add(user);
            await _userContext.SaveChangesAsync();
        }

        public string GenerateJSONWebToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            Claim[] claims = new Claim[] {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            };
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}