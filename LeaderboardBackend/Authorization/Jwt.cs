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
        private static TokenValidationParameters? s_Instance;

        public static TokenValidationParameters GetInstance(JwtConfig config)
        {
            if (s_Instance is not null)
            {
                return s_Instance;
            }

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(config.Key));
            s_Instance = new()
            {
                IssuerSigningKey = key,
                ValidAudience = config.Issuer,
                ValidIssuer = config.Issuer
            };

            return s_Instance;
        }
    }
}
