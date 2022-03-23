using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace LeaderboardBackend.Controllers.Annotations;

public static class Conventions
{
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public static void Get(
		[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
		object id,
		params object[] parameters)
	{ }

	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public static void Post(params object[] parameters)
	{ }
}
