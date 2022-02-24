namespace LeaderboardBackend.Controllers.Requests;

public record LoginRequest
{
	public string Email { get; set; } = null!;
	public string Password { get; set; } = null!;
}
