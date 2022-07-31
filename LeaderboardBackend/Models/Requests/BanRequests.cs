using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

public record CreateSiteBanRequest
{
	/// <summary>
	///     The ID of the user, who should be banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	///     The reason why the user is banned.
	/// </summary>
	[Required]
	public string Reason { get; set; } = null!;
};

public record CreateLeaderboardBanRequest
{
	/// <summary>
	///     The ID of the user, who should be banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	///     The reason why the user is banned.
	/// </summary>
	[Required]
	public string Reason { get; set; } = null!;

	/// <summary>
	///     The ID of the leaderboard on which the user should be banned.
	/// </summary>
	public long LeaderboardId { get; set; }
}
