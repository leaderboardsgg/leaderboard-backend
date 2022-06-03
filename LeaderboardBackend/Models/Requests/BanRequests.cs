using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

public record CreateSiteBanRequest
{
	/// <summary>
	/// The Id of the User, who should be banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	/// The Reason why the User is banned.
	/// </summary>
	[Required]
	public string Reason { get; set; } = null!;
};

public record CreateLeaderboardBanRequest
{
	/// <summary>
	/// The Id of the User, who should be banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	/// The Reason why the User is banned.
	/// </summary>
	[Required]
	public string Reason { get; set; } = null!;

	/// <summary>
	/// The Id of the Leaderboard on which the user should be banned.
	/// </summary>
	public long LeaderboardId { get; set; }
}
