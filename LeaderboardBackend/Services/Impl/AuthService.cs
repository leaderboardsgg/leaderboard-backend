using System.Security.Claims;
using System.Text;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Services;

public class AuthService : IAuthService
{
    private readonly SigningCredentials _credentials;
    private readonly string _issuer;

    public AuthService(IOptions<JwtConfig> options)
    {
        JwtConfig config = options.Value;
        SymmetricSecurityKey? securityKey = new(Encoding.UTF8.GetBytes(config.Key));

        _credentials = new(securityKey, SecurityAlgorithms.HmacSha256);
        _issuer = config.Issuer;
    }

    public string GenerateJSONWebToken(User user)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _issuer,
            Audience = _issuer,
            Claims = new Dictionary<string, object> {
                { JwtRegisteredClaimNames.Email, user.Email },
                { JwtRegisteredClaimNames.Sub, user.Id.ToString() }
            },
            Expires = DateTime.Now.AddMinutes(30),
            SigningCredentials = _credentials
        };

        return Jwt.SecurityTokenHandler.CreateToken(descriptor);
    }

    public string? GetEmailFromClaims(ClaimsPrincipal claims)
    {
        return claims.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    public Guid? GetUserIdFromClaims(ClaimsPrincipal claims)
    {
        string? userIdStr = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(userIdStr, out Guid userId))
        {
            return userId;
        }
        else
        {
            return null;
        }
    }
}
