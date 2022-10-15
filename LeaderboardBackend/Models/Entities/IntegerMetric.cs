using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a `IntegerMetric` for a `Category`.
/// </summary>
public class IntegerMetric : BaseEntity
{
	/// <summary>
	///     The unique identifier of the `IntegerMetric`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The display name of the `IntegerMetric`.
	/// </summary>
	/// <example>Foo</example>
	[NotNull]
	[Required]
	public string? Name { get; set; }

	/// <summary>
	///     The minimum value of the `IntegerMetric`. The default is 0.
	/// </summary>
	[Required]
	public long Min { get; set; }

	/// <summary>
	///     The maximum value of the `IntegerMetric`. The default is `Min` + 1.
	/// </summary>
	[Required]
	public long Max { get; set; }

	/// <summary>
	///     A collection of `Category`s scoped to the `IntegerMetric`.
	/// </summary>
	[MinLength(1)]
	public List<Category>? Categories { get; set; }

	/// <summary>
	///     A collection of `Run`s scoped to the `IntegerMetric`.
	/// </summary>
	public List<Run>? Runs { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is IntegerMetric metric
				&& base.Equals(metric)
				&& Id == metric.Id
				&& Name == metric.Name
				&& Min == metric.Min
				&& Max == metric.Max
				&& AreCategoriesEqual(metric)
				&& AreRunsEqual(metric);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Min, Max);
	}

	private bool AreCategoriesEqual(IntegerMetric comp)
	{
		if ((Categories is null || Categories.Count is 0) && (comp.Categories is null || comp.Categories.Count is 0))
		{
			return true;
		}

		return Categories is List<Category> ours
			&& comp.Categories is List<Category> theirs
			&& ours.ConvertAll(cat => cat.Id)
				.OrderBy(i => i)
				.SequenceEqual(
					theirs.ConvertAll(cat => cat.Id).OrderBy(i => i)
				);
	}

	private bool AreRunsEqual(IntegerMetric comp)
	{
		if ((Runs is null || Runs.Count is 0) && (comp.Runs is null || comp.Runs.Count is 0))
		{
			return true;
		}

		return Runs is List<Run> ours
			&& comp.Runs is List<Run> theirs
			&& ours.ConvertAll(cat => cat.Id)
				.OrderBy(i => i)
				.SequenceEqual(
					theirs.ConvertAll(cat => cat.Id).OrderBy(i => i)
				);
	}
}
