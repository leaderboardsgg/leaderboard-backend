using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.Requests;

public record CreateRunRequest
{
	[Required]
	public Instant Played { get; set; }

	[Required]
	public Instant Submitted { get; set; }

	[Required]
	public RunStatus Status { get; set; }
}
