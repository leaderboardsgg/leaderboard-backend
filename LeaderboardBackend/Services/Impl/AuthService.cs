using LeaderboardBackend.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LeaderboardBackend.Services;

public class AuthService : IAuthService
{
	private readonly SigningCredentials _credentials;
	private readonly string _issuer;

	public AuthService(IConfiguration config)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));

		_credentials = new(securityKey, SecurityAlgorithms.HmacSha256);
		_issuer = config["Jwt:Issuer"];
	}

	public string GenerateJSONWebToken(User user)
	{
		Claim[] claims =
		{
			new(JwtRegisteredClaimNames.Email, user.Email),
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
		};

		JwtSecurityToken token = new(
			issuer: _issuer,
			audience: _issuer,
			claims: claims,
			expires: DateTime.Now.AddMinutes(30),
			signingCredentials: _credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
