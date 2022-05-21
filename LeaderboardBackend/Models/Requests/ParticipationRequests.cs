using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

public record CreateParticipationRequest
{
	public string? Comment { get; set; }

	public string? Vod { get; set; }

	[Required]
	public Guid RunnerId { get; set; }

	[Required]
	public Guid RunId { get; set; }

	[Required]
	public bool IsSubmitter { get; set; } = true;
}

public record UpdateParticipationRequest
{
	public string Comment { get; set; } = "";

	// FIXME: Maybe we should make a custom rule for this such that it's both required
	// and that it has to be a link to a valid video, or a link from a set list of
	// domains.
	[Required(ErrorMessage = "Please add a VoD link. Your participation can't be confirmed otherwise.")]
	public string Vod { get; set; } = null!;
}
