using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Participation` for a `User` on a `Run`.
/// </summary>
public record CreateParticipationRequest
{
	/// <summary>
	///     An optional comment about the `Participation`.
	/// </summary>
	public string? Comment { get; set; }

	/// <summary>
	///     An optional link to video proof of the `Run`.
	/// </summary>
	public string? Vod { get; set; }

	/// <summary>
	///     The ID of the `User` who is participating.
	/// </summary>
	[Required]
	public Guid RunnerId { get; set; }

	/// <summary>
	///     The ID of the `Run` the `Participation` is created on.
	/// </summary>
	[Required]
	public Guid RunId { get; set; }

	/// <summary>
	///     Indicates whether the `Participation` is for the `User` who is creating it.
	/// </summary>
	[Required]
	public bool IsSubmitter { get; set; } = true;
}

/// <summary>
///     This request object is sent when updating a `Participation`.
/// </summary>
public record UpdateParticipationRequest
{
	/// <summary>
	///     A comment about the `Participation`.
	/// </summary>
	public string Comment { get; set; } = "";

	// FIXME: Maybe we should make a custom rule for this such that it's both required
	// and that it has to be a link to a valid video, or a link from a set list of
	// domains.
	/// <summary>
	///     A link to video proof of the `Run`.
	/// </summary>
	[Required(ErrorMessage = "Please add a VoD link. Your participation can't be confirmed otherwise.")]
	public string Vod { get; set; } = null!;
}
