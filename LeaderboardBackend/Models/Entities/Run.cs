using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     The status of the `Run`.<br/>
///         - 0: Created<br/>
///         - 1: Submitted<br/>
///         - 2: Pending<br/>
///         - 3: Approved<br/>
///         - 4: Rejected
/// </summary>
public enum RunStatus
{
	Created,
	Submitted,
	Pending,
	Approved,
	Rejected
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
	public LocalDate PlayedOn { get; set; }

	/// <summary>
	///     The time the request was made at.
	/// </summary>
	[Required]
	public Instant SubmittedAt { get; set; }

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

	/// <summary>
	///     A collection of `VariableValue`s on the `Run`.
	/// </summary>
	[Required]
	public List<VariableValue>? VariableValues { get; set; }
}
