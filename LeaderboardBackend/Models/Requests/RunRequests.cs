using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.Requests;

public record CreateRunRequest
{
	[Required]
	public DateTime Played { get; set; }

	[Required]
	public DateTime Submitted { get; set; }

	[Required]
	public RunStatus Status { get; set; }
}
