using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

public class CreateParticipationRequest
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

public class UpdateParticipationRequest
{
	public string Comment { get; set; } = "";

	[Required(ErrorMessage = "Please add a VoD link. Your participation can't be confirmed otherwise.")]
	public string Vod { get; set; } = null!;
}
