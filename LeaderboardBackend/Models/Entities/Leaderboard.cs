using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class Leaderboard
{
	public long Id { get; set; }

	[Required] 
	public string Name { get; set; } = null!;

	[Required] 
	public string Slug { get; set; } = null!;

	public string? Rules { get; set; }

	[JsonIgnore] 
	public List<Ban>? Bans { get; set; }

	[JsonIgnore] 
	public List<Category>? Categories { get; set; }

	[JsonIgnore] 
	public List<Modship>? Modships { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is Leaderboard leaderboard &&
			   Id == leaderboard.Id &&
			   Name == leaderboard.Name &&
			   Slug == leaderboard.Slug;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Slug);
	}
}
