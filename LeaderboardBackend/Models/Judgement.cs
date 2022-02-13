using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models
{
	public class Judgement
	{
		public long Id { get; set; }

		public bool? Approved { get; set; }

		[Required]
		public DateTime timestamp { get; set; }

		[Required]
		public string? Note { get; set; }

		[Required]
		public Guid ApproverId { get; set; }

		[Required]
		public User? Approver { get; set; }

		[Required]
		public Guid RunId { get; set; }

		[Required]
		public Run? Run { get; set; }
	}
}
