using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaderboardBackend.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Services;

public class AuthService : IAuthService
{
	private readonly SigningCredentials _credentials;
	private readonly string _issuer;

	public AuthService(IConfiguration config)
	{
		SymmetricSecurityKey? securityKey = new(Encoding.UTF8.GetBytes(config["Jwt:Key"]));

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

	public string? GetEmailFromClaims(ClaimsPrincipal claims)
	{
		return claims.FindFirstValue(JwtRegisteredClaimNames.Email);
	}

	public Guid? GetUserIdFromClaims(ClaimsPrincipal claims)
	{
		string? userIdStr = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

		if (userIdStr is null)
		{
			return null;
		}

		Guid? userId = null;

		try
		{
			userId = Guid.Parse(userIdStr);
		}
		catch (FormatException) { }

		return userId;
	}
}
