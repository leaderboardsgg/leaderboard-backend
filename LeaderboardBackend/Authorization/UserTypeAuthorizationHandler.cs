using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LeaderboardBackend.Authorization;

public class UserTypeAuthorizationHandler : AuthorizationHandler<UserTypeRequirement>
{
	private readonly IConfiguration _config;
	private readonly JwtSecurityTokenHandler _jwtHandler;
	private readonly TokenValidationParameters _jwtValidationParams;
	private readonly IUserService _userService;
	private readonly IModshipService _modshipService;

	public UserTypeAuthorizationHandler(
		IConfiguration config,
		IModshipService modshipService,
		IUserService userService
	)
	{
		_config = config;
		_jwtHandler = JwtSecurityTokenHandlerSingleton.Instance;
		_jwtValidationParams = TokenValidationParametersSingleton.Instance(config);
		_userService = userService;
		_modshipService = modshipService;
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
		AuthorizationHandlerContext _,
		UserTypeRequirement requirement
	) => requirement.Type switch
	{
		UserTypes.Admin => user.Admin,
		UserTypes.Mod => user.Admin || IsMod(user),
		UserTypes.User => true,
		_ => false,
	};

	private bool IsMod(User user) => _modshipService.LoadUserModships(user.Id).Result.Count() > 0;

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
			return _jwtHandler.CanReadToken(token);
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
			_jwtHandler.ValidateToken(
				token,
				_jwtValidationParams,
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
