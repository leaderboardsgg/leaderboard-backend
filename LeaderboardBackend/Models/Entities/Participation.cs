using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

public class Participation
{
	public long Id { get; set; }

	public string? Comment { get; set; }

	public string? Vod { get; set; }

	[Required]
	public Guid RunnerId { get; set; }

	[Required]
	public User? Runner { get; set; }

	[Required]
	public Guid RunId { get; set; }

	[Required]
	public Run? Run { get; set; }
}
