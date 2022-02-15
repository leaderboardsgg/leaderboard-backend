using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Controllers.Requests
{
	public class CreateParticipationRequest
	{
		public string? Comment { get; set; }

		public string? Vod { get; set; }

		[Required]
		public Guid RunnerId { get; set; }

		[Required]
		public long RunId { get; set; }

		[Required]
		public bool IsSubmitter { get; set; } = true;
	}
}
