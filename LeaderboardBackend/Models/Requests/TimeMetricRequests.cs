using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

public record CreateTimeMetricRequest
{
	/// <summary></summary>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>
	///   The minimum value for this metric. Expects the format hh:mm:ss, where:
	///   <ul>
	///     <li> <code>hh</code> means the number of hours from 00 to 9999;</li>
	///     <li> <code>mm</code> means the number of hours from 00 to 59; and</li>
	///     <li> <code>ss</code> means the number of hours from 00 to 59;</li>
	///   </ul>
	///   It also can't be larger than <code>Max</code>.
	/// </summary>
	[Required]
	[RegularExpression(@"\d{2,}:\d{2}:\d{2}")]
	public string Min { get; set; } = null!;

	/// <summary>
	///   The maximum value for this metric. Expects the format hh:mm:ss, where:
	///   <ul>
	///     <li> <code>hh</code> means the number of hours from 00 to 9999;</li>
	///     <li> <code>mm</code> means the number of hours from 00 to 59; and</li>
	///     <li> <code>ss</code> means the number of hours from 00 to 59;</li>
	///   </ul>
	///   It also can't be smaller than <code>Min</code>.
	/// </summary>
	[Required]
	[RegularExpression(@"\d{2,}:\d{2}:\d{2}")]
	public string Max { get; set; } = null!;
}
