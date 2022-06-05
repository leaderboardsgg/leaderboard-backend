using System.IdentityModel.Tokens.Jwt;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler : AuthorizationHandler<UserTypeRequirement>
{
	private readonly JwtSecurityTokenHandler JwtHandler;
	private readonly TokenValidationParameters JwtValidationParams;
	private readonly IUserService UserService;
	private readonly IModshipService ModshipService;
	private readonly IAuthService AuthService;

	public UserTypeAuthorizationHandler(
		IConfiguration config,
		IModshipService modshipService,
		IUserService userService,
		IAuthService authService
	)
	{
		JwtHandler = JwtSecurityTokenHandlerSingleton.Instance;
		JwtValidationParams = TokenValidationParametersSingleton.Instance(config);
		UserService = userService;
		ModshipService = modshipService;
		AuthService = authService;
	}

	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		UserTypeRequirement requirement
	)
	{
		if (!TryGetJwtFromHttpContext(context, out string token) || !ValidateJwt(token))
		{
			return Task.CompletedTask;
		}

		Guid? userId = AuthService.GetUserIdFromClaims(context.User);
		if (userId is null)
		{
			context.Fail();
			return Task.CompletedTask;
		}
		User? user = UserService.GetUserById(userId.Value).Result;

		if (user is null || !Handle(user, context, requirement))
		{
			// FIXME: Work out how to fail as a ForbiddenResult.
			context.Fail();
			return Task.CompletedTask;
		}

		context.Succeed(requirement);

		return Task.CompletedTask;
	}

	private bool Handle(
		User user,
		AuthorizationHandlerContext _,
		UserTypeRequirement requirement
	) => requirement.Type switch
	{
		UserTypes.Admin => user.Admin,
		UserTypes.Mod => user.Admin || IsMod(user),
		UserTypes.User => true,
		_ => false,
	};

	private bool IsMod(User user) => ModshipService.LoadUserModships(user.Id).Result.Count() > 0;

	private bool TryGetJwtFromHttpContext(AuthorizationHandlerContext context, out string token)
	{
		if (context.Resource is not HttpContext httpContext)
		{
			token = "";
			return false;
		}
		try
		{
			// We need to strip "Bearer " out lol
			token = httpContext.Request.Headers.Authorization.First().Substring(7);
			return JwtHandler.CanReadToken(token);
		} catch (InvalidOperationException)
		{
			// No token exists in the request
			token = "";
			return false;
		}
	}

	private bool ValidateJwt(string token)
	{
		try
		{
			JwtHandler.ValidateToken(
				token,
				JwtValidationParams,
				out _
			);
			return true;
		}
		// FIXME: Trigger a redirect to login, possibly on SecurityTokenExpiredException
		catch (Exception)
		{
			return false;
		}
	}
}
