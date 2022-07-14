using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public class TimeMetric
{
	/// <summary>Generated on creation.</summary>
	public long Id { get; set; }

	/// <summary>Name of the time metric.</summary>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>Minimum possible value.</summary>
	[Required]
	public Period Min { get; set; } = null!;

	/// <summary>Maximum possible value.</summary>
	public Period Max { get; set; } = null!;
}
