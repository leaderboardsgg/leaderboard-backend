using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a new `NumericalMetric` for a `Category`.
/// </summary>
public record CreateNumericalMetricRequest
{
	/// <summary>
	///     The name for the `NumericalMetric`. Must not be `null`.
	/// </summary>
	[Required]
	[MinLength(3)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The inclusive lower bound for the `NumericalMetric`. Defaults to 0.
	/// </summary>
	/// <example>0</example>
	public long? Min { get; set; } = 0;

	/// <summary>
	///     The inclusive upper bound for the `NumericalMetric`. Defaults to - and must at least be - `Min + 1`.
	/// </summary>
	/// <example>0</example>
	public long? Max { get; set; }

	/// <summary>
	///     The list of `Category` ID(s) to be associated with this `NumericalMetric`. Must not be empty.
	/// </summary>
	[Required]
	[MinLength(1, ErrorMessage = "You must tie at least one Category ID to this NumericalMetric.")]
	public long[] CategoryIds { get; set; } = null!;

	/// <summary>
	///     The list of `Run` ID(s) to be associated with this `NumericalMetric`. Must not be empty.
	/// </summary>
	[Required]
	[MinLength(1, ErrorMessage = "You must tie at least one Run ID to this NumericalMetric.")]
	public Guid[] RunIds { get; set; } = null!;
};

public record struct ParsedCreateNumericalMetricRequest
{
	public string Name;
	public long Min;
	public long Max;
	public long[] CategoryIds;
	public Guid[] RunIds;

	public static ParsedCreateNumericalMetricRequest Parse(CreateNumericalMetricRequest raw)
	{
		long min = 0, max = 1;
		if (raw.Min is null)
		{
			min = 0;
		}

		if (raw.Max is null)
		{
			max = min + 1;
		}

		return new()
		{
			Name = raw.Name,
			Min = min,
			Max = max,
			CategoryIds = raw.CategoryIds,
			RunIds = raw.RunIds,
		};
	}
}
