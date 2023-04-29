using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using Microsoft.Extensions.Options;
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
        Claim[] claims =
        {
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
        };

        JwtSecurityToken token =
            new(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: _credentials
            );

        return Jwt.SecurityTokenHandler.WriteToken(token);
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
