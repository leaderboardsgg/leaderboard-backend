using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class Judgement
{
	public long Id { get; set; }

	public bool? Approved { get; set; }

	[Required]
	public DateTime timestamp { get; set; }

	[Required]
	public string Note { get; set; } = null!;

	[Required]
	public Guid ApproverId { get; set; }

	[JsonIgnore]
	public User? Approver { get; set; }

	[Required]
	public Guid RunId { get; set; }

	[JsonIgnore]
	public Run? Run { get; set; } = null!;
}
