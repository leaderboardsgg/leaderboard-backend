using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public sealed class TokenValidationParametersSingleton
{
	private static TokenValidationParameters? parameters = null;

	public static TokenValidationParameters Instance(IConfiguration configuration)
	{
		if (parameters is not null)
		{
			return parameters;
		}
		SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
		parameters = new()
		{
			IssuerSigningKey = key,
			ValidAudience = configuration["Jwt:Issuer"],
			ValidIssuer = configuration["Jwt:Issuer"]
		};
		return parameters;
	}
}
