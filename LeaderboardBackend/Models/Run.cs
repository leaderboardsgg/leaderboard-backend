using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models
{
	public enum RunStatus {
		Created,
		Submitted,
		Pending,
		Approved,
		Rejected
	}

	public class Run
	{
		public long Id { get; set; }

		[Required]
		public DateTime? Played { get; set; }

		[Required]
		public DateTime? Submitted { get; set; }

		[Required]
		public RunStatus? Status { get; set; }

		[Required]
		public List<Participation>? Participations { get; set; }
	}
}
