using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Participations;

public class UpdateParticipationRequest
{
	public string Comment { get; set; } = "";

	[Required(ErrorMessage = "Please add a VoD link. Your participation can't be confirmed otherwise.")]
	public string Vod { get; set; } = null!;
}
