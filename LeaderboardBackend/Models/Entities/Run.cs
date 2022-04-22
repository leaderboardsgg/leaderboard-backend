using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

public enum RunStatus
{
	CREATED,
	SUBMITTED,
	PENDING,
	APPROVED,
	REJECTED
}

public class Run
{
	public Guid Id { get; set; }

	[Required]
	public DateTime Played { get; set; }

	[Required]
	public DateTime Submitted { get; set; }

	[Required]
	public RunStatus Status { get; set; }

	public List<Judgement>? Judgements { get; set; }

	[Required]
	public List<Participation>? Participations { get; set; }
}
