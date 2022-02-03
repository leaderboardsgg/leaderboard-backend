using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models;

public class Leaderboard
{
	public ulong Id { get; set; }
	[Required] public string Name { get; set; } = null!;
	[Required] public string Slug { get; set; } = null!;
	public string? Rules { get; set; }
	[JsonIgnore] public List<Modship>? Modships { get; set; }
}
