using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     0: Created
///     1: Submitted
///     2: Pending
///     3: Approved
///     4: Rejected
/// </summary>
public enum RunStatus
{
	CREATED,
	SUBMITTED,
	PENDING,
	APPROVED,
	REJECTED
}

/// <summary>
///     Represents an entry on a `Category`.
/// </summary>
public class Run
{
	/// <summary>
	///     The unique identifier of the `Run`.<br/>
	///     Generated on creation.
	/// </summary>
	public Guid Id { get; set; }

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

	/// <summary>
	///     A collection of `Judgement`s made about the `Run`.
	/// </summary>
	public List<Judgement>? Judgements { get; set; }

	/// <summary>
	///     A collection of `Participation`s on the `Run`.
	/// </summary>
	[Required]
	public List<Participation>? Participations { get; set; }
}
