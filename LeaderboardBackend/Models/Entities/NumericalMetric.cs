using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a `NumericalMetric` for a `Category`.
/// </summary>
public class NumericalMetric : BaseEntity
{
	/// <summary>
	///     The unique identifier of the `NumericalMetric`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The display name of the `NumericalMetric`.
	/// </summary>
	/// <example>Foo</example>
	[NotNull]
	[Required]
	public string? Name { get; set; }

	/// <summary>
	///     The minimum value of the `NumericalMetric`. The default is 0.
	/// </summary>
	[Required]
	public long Min { get; set; }

	/// <summary>
	///     The maximum value of the `NumericalMetric`. The default is `Min` + 1.
	/// </summary>
	[Required]
	public long Max { get; set; }

	/// <summary>
	///     A collection of `Category`s scoped to the `NumericalMetric`.
	/// </summary>
	[MinLength(1)]
	public List<Category>? Categories { get; set; }

	/// <summary>
	///     A collection of `Run`s scoped to the `NumericalMetric`.
	/// </summary>
	public List<Run>? Runs { get; set; }

	// Should we also match the equalities of Runs and Categories? - Shep
	public override bool Equals(object? obj)
	{
		return obj is NumericalMetric category
			&& Id == category.Id
			&& Name == category.Name
			&& Min == category.Min
			&& Max == category.Max;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Min, Max);
	}
}
