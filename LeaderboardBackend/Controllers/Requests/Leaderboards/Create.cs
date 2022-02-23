namespace LeaderboardBackend.Controllers.Requests;

public class CreateLeaderboardRequest
{
	public string Name { get; set; } = null!;
	public string Slug { get; set; } = null!;
}