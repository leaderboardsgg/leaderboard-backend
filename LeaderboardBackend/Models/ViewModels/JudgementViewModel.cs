using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>
///     Represents a decision made by a *Moderator* (`User`) about a `Run`.<br/>
///     See: <see cref="Judgement"/>.
/// </summary>
public record JudgementViewModel
{
	/// <summary>
	///     The unique identifier of the `Judgement`.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The `Judgement`'s decision. May be null, true, or false.<br/>
	///     `Note` will be non-empty when the decision is null or false.
	/// </summary>
	public bool? Approved { get; set; }

	/// <summary>
	///     The time the `Judgement` was made.
	/// </summary>
	/// <example>2022-01-01T12:34:56Z / 2022-01-01T12:34:56+01:00</example>
	public string? CreatedAt { get; set; }

	/// <summary>
	///     A comment elaborating on the `Judgement`'s decision. Will have a value when the
	///     affected `Run` is not approved (`Approved` is null or false).
	/// </summary>
	public string? Note { get; set; }

	/// <summary>
	///     The ID of the *Moderator* (`User`) who made the `Judgement`.
	/// </summary>
	public Guid ModId { get; set; }

	/// <summary>
	///     The ID of the `Run` which was judged.
	/// </summary>
	public Guid RunId { get; set; }

	public JudgementViewModel() { }

	public JudgementViewModel(Judgement judgement)
	{
		Id = judgement.Id;
		Approved = judgement.Approved;
		CreatedAt = judgement.CreatedAt.ToString();
		Note = judgement.Note;
		ModId = judgement.ModId;
		RunId = judgement.RunId;
	}
}
