using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaderboardBackend.Models;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Services
{
	public class AuthService : IAuthService
	{
		private SigningCredentials _credentials;
		private string _issuer;

		public AuthService(IConfiguration config)
		{
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
			_credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
			_issuer = config["Jwt:Issuer"];
		}

		public string GenerateJSONWebToken(User user)
		{
			Claim[] claims = new Claim[] {
				new Claim(JwtRegisteredClaimNames.Email, user.Email!), // nullable reference warning false positive
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			};
			var token = new JwtSecurityToken(
				_issuer,
				_issuer,
				claims,
				expires: DateTime.Now.AddMinutes(30),
				signingCredentials: _credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);

		}
	}
}
