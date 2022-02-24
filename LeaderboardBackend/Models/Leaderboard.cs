using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models;

public class Leaderboard
{
	public ulong Id { get; set; }
	[Required] public string Name { get; set; } = null!;
	[Required] public string Slug { get; set; } = null!;
	public string Rules { get; set; } = null!;
}
