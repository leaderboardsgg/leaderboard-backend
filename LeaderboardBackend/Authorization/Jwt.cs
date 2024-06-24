using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

/// <summary>
///    The <see cref="Jwt"/> class acts as a <see langword="static"/> helper for JSON Web Token
///    related data.
/// </summary>
public static class Jwt
{
    public static JsonWebTokenHandler SecurityTokenHandler { get; } = new();

    public static class ValidationParameters
    {
        private static TokenValidationParameters? _instance;

        public static TokenValidationParameters GetInstance(JwtConfig config)
        {
            if (_instance is not null)
            {
                return _instance;
            }

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(config.Key));
            _instance = new()
            {
                IssuerSigningKey = key,
                ValidAudience = config.Issuer,
                ValidIssuer = config.Issuer
            };

            return _instance;
        }
    }
}
