using System;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Helpers;

internal static class ObjectResultHelpers
{
	public static void AssertResponseNotFound<T>(ActionResult<T> response)
	{
		var actual = response.Result as NotFoundResult;

		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
	}

	public static void AssertResponseBadRequest<T>(ActionResult<T> response)
	{
		var actual = response.Result as BadRequestResult;

		Assert.NotNull(actual);
		Assert.AreEqual(400, actual!.StatusCode);
	}

	public static void AssertResponseForbid<T>(ActionResult<T> response)
	{
		var actual = response.Result as ForbidResult;

		Assert.NotNull(actual);
		// ForbidResult inherits from ActionResult, not StatusCodeResult. Unlike
		// the previous assertions, this is sufficient to know we have received
		// a Forbidden 403 result.
	}

	public static TValue? GetValueFromObjectResult<TValue, TObjectResult>(ActionResult<TValue> result) where TObjectResult : ObjectResult
	{
		TObjectResult? objectResult = null;
		try
		{
			objectResult = (TObjectResult?)(result?.Result);
		}
		catch (InvalidCastException ex)
		{
			Assert.Fail(ex.Message);
		}

		return (TValue?)(objectResult?.Value);
	}

}
