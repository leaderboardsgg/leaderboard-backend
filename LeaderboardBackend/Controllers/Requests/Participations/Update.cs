namespace LeaderboardBackend.Controllers.Requests
{
	public class UpdateParticipationRequest
	{
		public string Comment { get; set; } = null!;

		public string Vod { get; set; } = null!;
	}
}
