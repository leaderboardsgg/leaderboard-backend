using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
/// 0: Created
/// 1: Submitted
/// 2: Pending
/// 3: Approved
/// 4: Rejected
/// </summary>
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
	public Instant Played { get; set; }

	[Required]
	public Instant Submitted { get; set; }

	[Required]
	public RunStatus Status { get; set; }

	public List<Judgement>? Judgements { get; set; }

	[Required]
	public List<Participation>? Participations { get; set; }
}
