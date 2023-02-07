using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Authorization;

public class JwtConfig
{
	public const string KEY = "Jwt";

	[Required]
	public required string Key { get; set; }
	[Required]
	public required string Issuer { get; set; }
}
