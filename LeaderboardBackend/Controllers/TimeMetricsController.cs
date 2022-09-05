using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TimeMetricsController : ControllerBase
{
	private readonly ITimeMetricService TimeMetricService;

	public TimeMetricsController(ITimeMetricService service)
	{
		TimeMetricService = service;
	}

	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesDefaultResponseType]
	public async Task<ActionResult<Models.Entities.TimeMetric>> GetTimeMetric(long id)
	{
		TimeMetric? timeMetric = await TimeMetricService.GetTimeMetric(id);
		if (timeMetric is null)
		{
			return NotFound();
		}

		return Ok(timeMetric);
	}

	public async Task<ActionResult> CreateTimeMetric([FromBody] CreateTimeMetricRequest request)
	{
		if (!Parser.CreateTimeMetric(request, out TimeMetric? metric, out string? error))
		{
			return ValidationProblem($"Validation error: {error}");
		}

		await TimeMetricService.CreateTimeMetric(metric);

		return Ok();
	}
}

internal static class Parser
{
	public static bool CreateTimeMetric(
		CreateTimeMetricRequest from,
		[NotNullWhen(true)] out TimeMetric? parsed,
		[NotNullWhen(false)] out string? error
	)
	{
		Period? min = getPeriod(from.Min);
		Period? max = getPeriod(from.Max);

		if (min is null)
		{
			parsed = null;
			error = $"Invalid minimum time format; should be HH:mm:ss. Received: ${from.Min}";
			return false;
		}

		if (max is null)
		{
			parsed = null;
			error = $"Invalid maximum time format; should be HH:mm:ss. Received: ${from.Max}";
			return false;
		}

		parsed = new TimeMetric
		{
			Max = max,
			Min = min,
			Name = from.Name,
		};
		error = null;
		return true;
	}

	private static Period? getPeriod(string from)
	{
		Match m = Regex.Match(@"(\d{2,}):(\d{2}):(\d{2})", from, RegexOptions.Compiled);
		if (!m.Success)
		{
			return null;
		};

		PeriodBuilder builder = new();
		builder.Hours = int.Parse(m.Groups[1].Value);
		builder.Minutes = int.Parse(m.Groups[2].Value);
		builder.Seconds = int.Parse(m.Groups[3].Value);

		return builder.Build();
	}
}
