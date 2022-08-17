using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents the participation of a `User` on a `Run`.
/// </summary>
public class Participation
{
	/// <summary>
	///     The unique identifier of the `Participation`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     An optional comment about the `Participation`.
	/// </summary>
	public string? Comment { get; set; }

	/// <summary>
	///     An optional link to video proof of the `Run`.
	/// </summary>
	public string? Vod { get; set; }

	/// <summary>
	///     The ID of the `User` who is participating.
	/// </summary>
	[Required]
	public Guid RunnerId { get; set; }

	[Required]
	public User? Runner { get; set; }

	/// <summary>
	///     The ID of the `Run` the `Participation` is created on.
	/// </summary>
	[Required]
	public Guid RunId { get; set; }

	[Required]
	public Run? Run { get; set; }
}
