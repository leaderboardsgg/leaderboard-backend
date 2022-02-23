namespace LeaderboardBackend.Controllers.Requests;

public class RegisterRequest
{
	public string Username { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Password { get; set; } = null!;
	public string PasswordConfirm { get; set; } = null!;
}
