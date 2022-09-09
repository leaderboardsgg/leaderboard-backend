using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler : AuthorizationHandler<UserTypeRequirement>
{
	private readonly IAuthService _authService;
	private readonly TokenValidationParameters _jwtValidationParams;
	private readonly IModshipService _modshipService;
	private readonly IUserService _userService;

	public UserTypeAuthorizationHandler(
		IAuthService authService,
		IConfiguration config,
		IModshipService modshipService,
		IUserService userService)
	{
		_authService = authService;
		_jwtValidationParams = Jwt.ValidationParameters.GetInstance(config);
		_modshipService = modshipService;
		_userService = userService;
	}

	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		UserTypeRequirement requirement)
	{
		if (!TryGetJwtFromHttpContext(context, out string? token) || !ValidateJwt(token))
		{
			return Task.CompletedTask;
		}

		Guid? userId = _authService.GetUserIdFromClaims(context.User);

		if (userId is null)
		{
			context.Fail();
			return Task.CompletedTask;
		}

		User? user = _userService.GetUserById(userId.Value).Result;

		if (user is null || !Handle(user, requirement))
		{
			// FIXME: Work out how to fail as a ForbiddenResult.
			context.Fail();
			return Task.CompletedTask;
		}

		context.Succeed(requirement);

		return Task.CompletedTask;
	}

	private bool Handle(User user, UserTypeRequirement requirement)
	{
		return requirement.Type switch
		{
			UserTypes.ADMINISTRATOR => user.Admin,
			UserTypes.MODERATOR => user.Admin || IsMod(user),
			UserTypes.USER => true,
			_ => false,
		};
	}

	private bool IsMod(User user)
	{
		return _modshipService.LoadUserModships(user.Id).Result.Count > 0;
	}

	private static bool TryGetJwtFromHttpContext(
		AuthorizationHandlerContext context,
		[NotNullWhen(true)] out string? token)
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

	private bool ValidateJwt(string token)
	{
		try
		{
			Jwt.SecurityTokenHandler.ValidateToken(token, _jwtValidationParams, out _);

			return true;
		}
		// FIXME: Trigger a redirect to login, possibly on SecurityTokenExpiredException
		catch
		{
			return false;
		}
	}
}
