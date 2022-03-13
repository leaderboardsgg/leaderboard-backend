using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace LeaderboardBackend.Controllers;

public static class Conventions
{
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Get(
		[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
		object id,
		params object[] parameters)
	{ }

	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Create(params object[] parameters)
	{ }
}
