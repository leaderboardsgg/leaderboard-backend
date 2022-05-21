using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

/// <summary>A decision by a mod on a run submission.</summary>
/// <remarks>
/// The latest judgement on a run updates its status.
/// A judgement can be one of these three types: <br/>
/// - an approval if Approval == true; <br/>
/// - a rejection if Approval == false; and <br/>
/// - a comment if Approval == null. <br/>
/// Judgements are NOT created if: <br/>
/// - its related run has "CREATED" status; <br/>
/// - its Note is empty while its Approved is false or null. <br/>
/// I.e. for the second point, a mod MUST add a note if they want to reject or simply comment on a submission. <br/>
/// Moderators CANNOT modify their judgements once made.
/// </remarks>
public class Judgement
{
	/// <summary>Generated on creation.</summary>
	public long Id { get; set; }

	/// <summary>
	/// Defines this judgement, which in turn defines the status of its related run. <br />
	/// If:
	///   <ul>
	///     <li>true, run is approved;</li>
	///     <li>false, run is rejected;</li>
	///     <li>null, run is commented on.</li>
	///   </ul>
	/// For the latter two, Note MUST be non-empty.
	/// </summary>
	public bool? Approved { get; set; }

	/// <summary>When the judgement was made.</summary>
	[Required]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Comments on the judgement.
	/// MUST be non-empty for rejections or comments (Approved ∈ {false, null}).
	/// </summary>
	[Required]
	public string Note { get; set; } = "";

	/// <summary>ID of the mod that made this judgement.</summary>
	[Required]
	public Guid ModId { get; set; }

	/// <summary>Model of the mod that made this judgement.</summary>
	[Required]
	public User Mod { get; set; } = null!;

	/// <summary>ID of the related run.</summary>
	[Required]
	public Guid RunId { get; set; }

	/// <summary>Model of the related run.</summary>
	[Required]
	public Run Run { get; set; } = null!;
}
