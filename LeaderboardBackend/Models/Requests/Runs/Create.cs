using LeaderboardBackend.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Run;

public class CreateRunRequest
{
	[Required]
	public DateTime Played { get; set; }

	[Required]
	public DateTime Submitted { get; set; }

	[Required]
	public RunStatus Status { get; set; }
}
