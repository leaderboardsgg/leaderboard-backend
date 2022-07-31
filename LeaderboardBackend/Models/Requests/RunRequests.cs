using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Run`.
/// </summary>
public record CreateRunRequest
{
	/// <summary>
	///     The date the `Run` was played on.
	/// </summary>
	[Required]
	public Instant Played { get; set; }

	/// <summary>
	///     The time the request was made at.
	/// </summary>
	[Required]
	public Instant Submitted { get; set; }

	/// <summary>
	///     The current status of the `Run` creation.
	/// </summary>
	[Required]
	public RunStatus Status { get; set; }
}
