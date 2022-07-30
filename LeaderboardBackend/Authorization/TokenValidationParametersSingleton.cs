using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public sealed class TokenValidationParametersSingleton
{
	private static TokenValidationParameters? s_Parameters;

	public static TokenValidationParameters Instance(IConfiguration configuration)
	{
		if (s_Parameters is not null)
		{
			return s_Parameters;
		}

		SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
		s_Parameters = new()
		{
			IssuerSigningKey = key,
			ValidAudience = configuration["Jwt:Issuer"],
			ValidIssuer = configuration["Jwt:Issuer"]
		};

		return s_Parameters;
	}
}
