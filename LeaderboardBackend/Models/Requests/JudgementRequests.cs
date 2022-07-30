using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     Request object sent when creating a Judgement.
/// </summary>
public record CreateJudgementRequest
{

	/// <summary>
	///     GUID of the run.
	/// </summary>
	/// <example>e1c010d7-b499-4196-be17-aa27a053f3dc</example>
	public Guid RunId { get; set; }

	/// <summary>
	///     Judgement comments. Must be provided if not outright approving a run ("Approved" is
	///     false or null).
	///     Acts as mod feedback for the runner.
	/// </summary>
	/// <example>e1c010d7-b499-4196-be17-aa27a053f3dc</example>
	[Note]
	public string Note { get; set; } = null!;

	/// <summary>
	///     The judgement result. Can be true, false, or null. For the latter two, "Note" must be
	///     non-empty.
	/// </summary>
	/// <example>e1c010d7-b499-4196-be17-aa27a053f3dc</example>
	public bool? Approved { get; set; }
}
