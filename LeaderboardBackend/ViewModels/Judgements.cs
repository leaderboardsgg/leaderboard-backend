using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.ViewModels;

/// <summary>A decision by a mod on a run submission. See Models/Entities/Judgement.cs.</summary>
/// <param name="Id">The newly-made judgement's ID.</param>
/// <param name="Approved">
///   The judgement result. Can be true, false, or null. In the latter two, Note will be non-empty.
/// </param>
/// <param name="CreatedAt">
///   When the judgement was made. Follows <a href="https://www.ietf.org/rfc/rfc3339.txt">RFC 3339</a>.
///   E.g. 2022-01-01T12:34:56Z / 2022-01-01T12:34:56+01:00
/// </param>
/// <param name="Note">
///   Judgement comments. Acts as mod feedback for the runner. Will be non-empty for
///   non-approval judgements (Approved is false or null).
/// </param>
/// <param name="ModId">ID of mod who made this judgement.</param>
/// <param name="RunId">ID of run this judgement's for.</param>
public readonly record struct JudgementViewModel
(
	long Id,
	bool? Approved,
	string CreatedAt,
	string? Note,
	Guid ModId,
	Guid RunId
)
{
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
