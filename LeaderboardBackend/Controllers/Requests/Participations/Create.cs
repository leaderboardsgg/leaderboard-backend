namespace LeaderboardBackend.Controllers.Requests
{
	public class CreateParticipationRequest
	{
		public string? Comment { get; set; }

		public string? Vod { get; set; }

		public Guid RunnerId { get; set; }

		public long RunId { get; set; }

		public bool IsRunner { get; set; } = true;
	}
}
