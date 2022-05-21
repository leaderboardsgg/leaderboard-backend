using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>A decision by a mod on a run submission. See Models/Entities/Judgement.cs.</summary>
public record JudgementViewModel
{
	/// <summary>The newly-made judgement's ID.</summary>
	public long Id { get; set; }

	/// <summary>
	///   The judgement result. Can be true, false, or null. In the latter two, <code>Note</code> will be non-empty.
	/// </summary>
	public bool? Approved { get; set; }

	/// <summary>
	///   When the judgement was made. Follows <a href="https://www.ietf.org/rfc/rfc3339.txt">RFC 3339</a>.
	/// </summary>
	/// <example>2022-01-01T12:34:56Z / 2022-01-01T12:34:56+01:00</example>
	public string CreatedAt { get; set; }

	/// <summary>
	///   Judgement comments. Acts as mod feedback for the runner. Will be non-empty for
	///   non-approval judgements (Approved is false or null).
	/// </summary>
	public string? Note { get; set; }

	/// <summary>ID of mod who made this judgement.</summary>
	public Guid ModId { get; set; }

	/// <summary>ID of run this judgement's for.</summary>
	public Guid RunId { get; set; }

	public JudgementViewModel() { }

	public JudgementViewModel(Judgement judgement)
	{
		Id = judgement.Id;
		Approved = judgement.Approved;
		CreatedAt = judgement.CreatedAt.ToLongDateString();
		Note = judgement.Note;
		ModId = judgement.ModId;
		RunId = judgement.RunId;
	}
}
