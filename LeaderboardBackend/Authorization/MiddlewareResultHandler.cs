using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LeaderboardBackend.Authorization;

public class MiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
	private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

	public async Task HandleAsync(
		RequestDelegate requestDelegate,
		HttpContext httpContext,
		AuthorizationPolicy authorizationPolicy,
		PolicyAuthorizationResult policyAuthorizationResult)
	{
		if (policyAuthorizationResult.Forbidden)
		{
			httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
			return;
		}

		// Fallback to the default implementation.
		// This can mean calling the controller action itself.
		await _defaultHandler.HandleAsync(
			requestDelegate,
			httpContext,
			authorizationPolicy,
			policyAuthorizationResult);
	}
}
