using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a new `NumericalMetric` for a `Category`.
/// </summary>
public record CreateNumericalMetricRequest
{
	/// <summary>
	///     The name for the `NumericalMetric`. Must not be empty.
	/// </summary>
	[Required]
	[MinLength(3)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The inclusive lower bound for the `NumericalMetric`. Defaults to 0.
	/// </summary>
	/// <example>0</example>
	public long Min { get; set; } = 0;

	/// <summary>
	///     The inclusive upper bound for the `NumericalMetric`. Defaults to - and must at least be - `Min + 1`.
	/// </summary>
	/// <example>0</example>
	public long? Max { get; set; }

	/// <summary>
	///     The list of `Category` ID(s) to be associated with this `NumericalMetric`. Must not be empty.
	/// </summary>
	[Required(ErrorMessage = "You must pass at least one Category ID to this NumericalMetric.")]
	[MinLength(1, ErrorMessage = "You must pass at least one Category ID to this NumericalMetric.")]
	public long[] CategoryIds { get; set; } = null!;

	/// <summary>
	///     The list of `Run` ID(s) to be associated with this `NumericalMetric`. Can be empty.
	/// </summary>
	public Guid[] RunIds { get; set; } = { };
};

public record struct ParsedCreateNumericalMetricRequest
{
	public string Name;
	public long Min;
	public long Max;
	public long[] CategoryIds;
	public Guid[] RunIds;

	public ParsedCreateNumericalMetricRequest(CreateNumericalMetricRequest raw)
	{
		Name = raw.Name;
		Min = raw.Min;
		Max = raw.Max ?? raw.Min + 1;
		CategoryIds = raw.CategoryIds;
		RunIds = raw.RunIds;
	}
}
