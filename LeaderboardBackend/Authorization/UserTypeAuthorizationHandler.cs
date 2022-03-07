using LeaderboardBackend.Authorization.Requirements;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler : AuthorizationHandler<UserTypeRequirement>
{
	private readonly IConfiguration _config;
	private readonly JwtSecurityTokenHandler _jwtHandler;
	private readonly TokenValidationParameters _jwtValidationParams;
	private readonly IUserService _userService;

	public UserTypeAuthorizationHandler(
		IConfiguration config,
		IModshipService modshipService,
		IUserService userService
	)
	{
		_config = config;
		_jwtHandler = JwtSecurityTokenHandlerSingleton.Instance;

		// FIXME: Make _jwtValidationParams take from a singleton too
		SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
		_jwtValidationParams = new()
		{
			IssuerSigningKey = key,
			ValidAudience = config["Jwt:Issuer"],
			ValidIssuer = config["Jwt:Issuer"]
		};
		_userService = userService;
	}

	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		UserTypeRequirement requirement
	)
	{
		if (!CanGetJwt(context, out string token) || !CanValidateJwt(token))
		{
			return Task.CompletedTask;
		}

		User? user = _userService.GetUserFromClaims(context.User).Result;

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
		AuthorizationHandlerContext context,
		UserTypeRequirement requirement
	) => requirement.Type switch
	{
		UserTypes.Admin => IsAdmin(user),
		UserTypes.Mod => IsMod(user),
		UserTypes.User => true,
		_ => false,
	};

	// FIXME: Rebase PR which adds Admin prop to Users
	private bool IsAdmin(User user) => false;

	// FIXME: Users don't get automagically populated with Modships when on creation of the latter.
	private bool IsMod(User user) => user.Modships?.Count() > 0;

	private bool CanGetJwt(AuthorizationHandlerContext context, out string token)
	{
		if (context.Resource is not HttpContext httpContext)
		{
			token = "";
			return false;
		}
		// We need to strip "Bearer " out lol
		token = httpContext.Request.Headers.Authorization.First().Substring(7);
		return _jwtHandler.CanReadToken(token);
	}

	private bool CanValidateJwt(string token)
	{
		try
		{
			_jwtHandler.ValidateToken(
				token,
				_jwtValidationParams,
				out _
			);
			return true;
		}
		// FIXME: Trigger a redirect to login, possibly on SecurityTokenExpiredException
		catch (System.Exception)
		{
			return false;
		}
	}
}
