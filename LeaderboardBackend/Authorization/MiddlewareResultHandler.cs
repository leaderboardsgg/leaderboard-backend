using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LeaderboardBackend.Authorization;

public class MiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
	private readonly AuthorizationMiddlewareResultHandler DefaultHandler =
		 new();

	public async Task HandleAsync(
		RequestDelegate requestDelegate,
		HttpContext httpContext,
		AuthorizationPolicy authorizationPolicy,
		PolicyAuthorizationResult policyAuthorizationResult)
	{
		// FIXME: For now, we're doing a simple pass-through. In the future we'd want to
		// conditionally return a redirect to the login page. -zysim

		// Fallback to the default implementation.
		// This can mean calling the controller action itself.
		await DefaultHandler.HandleAsync(requestDelegate, httpContext, authorizationPolicy,
							   policyAuthorizationResult);
	}
}
