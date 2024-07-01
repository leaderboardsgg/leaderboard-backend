using System.Diagnostics.CodeAnalysis;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler : AuthorizationHandler<UserTypeRequirement>
{
    private readonly IAuthService _authService;
    private readonly TokenValidationParameters _jwtValidationParams;
    private readonly IUserService _userService;

    public UserTypeAuthorizationHandler(
        IAuthService authService,
        IOptions<JwtConfig> config,
        IUserService userService
    )
    {
        _authService = authService;
        _jwtValidationParams = Jwt.ValidationParameters.GetInstance(config.Value);
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserTypeRequirement requirement
    )
    {
        if (!TryGetJwtFromHttpContext(context, out string? token) || !await ValidateJwt(token))
        {
            return;
        }

        Guid? userId = _authService.GetUserIdFromClaims(context.User);

        if (userId is null)
        {
            context.Fail();
            return;
        }

        User? user = _userService.GetUserById(userId.Value).Result;

        if (user is null || !Handle(user, requirement))
        {
            // FIXME: Work out how to fail as a ForbiddenResult.
            context.Fail();
            return;
        }

        context.Succeed(requirement);

        return;
    }

    private bool Handle(User user, UserTypeRequirement requirement)
    {
        return requirement.Type switch
        {
            UserTypes.ADMINISTRATOR => user.IsAdmin,
            UserTypes.USER => true,
            _ => false,
        };
    }
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
