using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class Category
{
	public long Id { get; set; }

	[Required] 
	public string Name { get; set; } = null!;

	[Required] 
	public string Slug { get; set; } = null!;
	
	public string? Rules { get; set; }

	[Required] 
	public int PlayersMin { get; set; }

	[Required] 
	public int PlayersMax { get; set; }

	[Required] 
	public long LeaderboardId { get; set; }

	[JsonIgnore] 
	public Leaderboard? Leaderboard { get; set; }
}
