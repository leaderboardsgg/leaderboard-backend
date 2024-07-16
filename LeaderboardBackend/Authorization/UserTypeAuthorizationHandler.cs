using System.Diagnostics.CodeAnalysis;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler(
    IOptions<JwtConfig> config,
    IUserService userService
    ) : AuthorizationHandler<UserTypeRequirement>
{
    private readonly TokenValidationParameters _jwtValidationParams = Jwt.ValidationParameters.GetInstance(config.Value);
    private readonly IUserService _userService = userService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserTypeRequirement requirement
    )
    {
        if (!TryGetJwtFromHttpContext(context, out string? token) || !await ValidateJwt(token))
        {
            return;
        }

        GetUserResult res = await _userService.GetUserFromClaims(context.User);

        res.Switch(user =>
        {
            if (Handle(user, requirement))
            {
                context.Succeed(requirement);
            }
        }, badCredentials => context.Fail(new(this, "Bad Credentials")), notFound => context.Fail(new(this, "User Not Found")));
    }

    private static bool Handle(User user, UserTypeRequirement requirement) => requirement.Type switch
    {
        UserTypes.ADMINISTRATOR => user.IsAdmin,
        UserTypes.USER => true,
        _ => false,
    };
    private static bool TryGetJwtFromHttpContext(
        AuthorizationHandlerContext context,
        [NotNullWhen(true)] out string? token
    )
    {
        if (context.Resource is not HttpContext httpContext)
        {
            token = null;
            return false;
        }

        string? header = httpContext.Request.Headers.Authorization.FirstOrDefault();

        if (header is null)
        {
            token = null;
            return false;
        }

        token = header.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            token = null;
            return false;
        }

        return Jwt.SecurityTokenHandler.CanReadToken(token);
    }

    private async Task<bool> ValidateJwt(string token)
    {
        TokenValidationResult result = await Jwt.SecurityTokenHandler.ValidateTokenAsync(token, _jwtValidationParams);
        return result.IsValid;
    }
}
