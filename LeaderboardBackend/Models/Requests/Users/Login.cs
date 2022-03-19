using LeaderboardBackend.Models.Annotations;
<<<<<<< HEAD:LeaderboardBackend/Models/Requests/Users/Login.cs
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Users;

public record LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;
}

public record LoginResponse
{
	[Required]
	public string Token { get; set; } = null!;
}
=======
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Users;

public record LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;
}
>>>>>>> 4f98eea... Restructured Models, added Models from all PRs:LeaderboardBackend/Controllers/Requests/Login.cs
