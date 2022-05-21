using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.ViewModels;

/// <summary>A decision by a mod on a run submission. See Models/Entities/Judgement.cs.</summary>
public record JudgementViewModel(long Id, bool? Approved, string CreatedAt, string? Note, Guid ModId, Guid RunId)
{
	/// <summary>The newly-made judgement's ID.</summary>
	internal long Id { get; set; } = Id;

	/// <summary>
	///   The judgement result. Can be true, false, or null. In the latter two, <code>Note</code> will be non-empty.
	/// </summary>
	internal bool? Approved { get; set; } = Approved;

	/// <summary>
	///   When the judgement was made. Follows <a href="https://www.ietf.org/rfc/rfc3339.txt">RFC 3339</a>.
	/// </summary>
	/// <example>2022-01-01T12:34:56Z / 2022-01-01T12:34:56+01:00</example>
	internal string CreatedAt { get; set; } = CreatedAt;

	/// <summary>
	///   Judgement comments. Acts as mod feedback for the runner. Will be non-empty for
	///   non-approval judgements (Approved is false or null).
	/// </summary>
	internal string? Note { get; set; } = Note;

	/// <summary>ID of mod who made this judgement.</summary>
	internal Guid ModId { get; set; } = ModId;

	/// <summary>ID of run this judgement's for.</summary>
	internal Guid RunId { get; set; } = RunId;

	public JudgementViewModel(Judgement judgement) : this
	(
		judgement.Id,
		judgement.Approved,
		judgement.CreatedAt.ToLongDateString(),
		judgement.Note,
		judgement.ModId,
		judgement.RunId
	) {}
}
