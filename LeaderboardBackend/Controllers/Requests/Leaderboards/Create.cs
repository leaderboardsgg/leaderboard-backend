namespace LeaderboardBackend.Controllers.Requests;

public record CreateLeaderboardRequest
{
	[Required] public string Name { get; set; } = null!;
	[Required] public string Slug { get; set; } = null!;
	public string? Rules { get; set; }
}
