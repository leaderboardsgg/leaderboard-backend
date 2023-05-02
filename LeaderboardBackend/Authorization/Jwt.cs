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
    public static JwtSecurityTokenHandler SecurityTokenHandler { get; } = new();

    public static class ValidationParameters
    {
        private static TokenValidationParameters? s_instance;

        public static TokenValidationParameters GetInstance(JwtConfig config)
        {
            if (s_instance is not null)
            {
                return s_instance;
            }

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(config.Key));
            s_instance = new()
            {
                IssuerSigningKey = key,
                ValidAudience = config.Issuer,
                ValidIssuer = config.Issuer
            };

            return s_instance;
        }
    }
}
