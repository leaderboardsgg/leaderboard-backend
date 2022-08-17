using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Judgement`.
/// </summary>
public record CreateJudgementRequest
{
	/// <summary>
	///     The ID of the `Run` that is being judged.
	/// </summary>
	public Guid RunId { get; set; }

	/// <summary>
	///     A comment elaborating on the `Judgement`'s decision. Must have a value when the
	///     affected `Run` is not approved (`Approved` is null or false).
	/// </summary>
	/// <example>The video proof is not of sufficient quality.</example>
	[Note]
	public string Note { get; set; } = null!;

	/// <summary>
	///     The `Judgement`'s decision. May be null, true, or false.
	/// </summary>
	public bool? Approved { get; set; }
}
