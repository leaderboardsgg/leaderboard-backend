using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Participations;

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

