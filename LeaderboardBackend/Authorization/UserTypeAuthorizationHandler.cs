using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LeaderboardBackend.Authorization;

// FIXME: This should be reimplemented with the role-based authz thingy:
// https://github.com/leaderboardsgg/leaderboard-backend/issues/104
// - zysim
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
			return Fail(context);
		}

		Guid? userId = AuthService.GetUserIdFromClaims(context.User);
		if (userId is null)
		{
			return Fail(context);
		}

		User? user = UserService.GetUserById(userId.Value).Result;
		if (user is null)
		{
			return Fail(context);
		}

		AddRoleClaimToContext(user, context);

		if (!IsAuthorized(context, requirement))
		{
			return Fail(context);
		}

		context.Succeed(requirement);

		return Task.CompletedTask;
	}

	private void AddRoleClaimToContext(User user, AuthorizationHandlerContext context)
	{
		if (user.Admin)
		{
			context.User.AddIdentity(new(new Claim[] { new("role", UserTypes.Admin) }));
			return;
		}
		if (IsMod(user))
		{
			context.User.AddIdentity(new(new Claim[] { new("role", UserTypes.Mod) }));
			return;
		}
	}

	private bool IsAuthorized(
		AuthorizationHandlerContext context,
		UserTypeRequirement requirement
	) => context.User.HasClaim("role", requirement.Type) || context.User.HasClaim("role", UserTypes.Admin);

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

	private Task Fail(AuthorizationHandlerContext context, string? reason = null)
	{
		if (reason is not null)
		{
			context.Fail(new(this, reason));
		} else
		{
			context.Fail();
		}
		return Task.CompletedTask;
	}
}
