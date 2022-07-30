using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

/// <summary>
///    The <see cref="Jwt"/> class acts as a <see langword="static"/> helper for JSON Web Token
///    related data.
/// </summary>
public static class Jwt
{
	public const string KEY = "Jwt:Key";
	public const string ISSUER = "Jwt:Issuer";

	public static JwtSecurityTokenHandler SecurityTokenHandler { get; } = new();

	public static class ValidationParameters
	{
		private static TokenValidationParameters? s_Instance;

		public static TokenValidationParameters GetInstance(IConfiguration configuration)
		{
			if (s_Instance is not null)
			{
				return s_Instance;
			}

			SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration[KEY]));
			s_Instance = new()
			{
				IssuerSigningKey = key,
				ValidAudience = configuration[ISSUER],
				ValidIssuer = configuration[ISSUER]
			};

			return s_Instance;
		}
	}
}
