using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Entities;

/// <summary>A decision by a mod on a run submission.</summary>
/// <remarks>
/// The latest judgement on a run updates its status.
/// A judgement can be one of these three types:
/// <ol>
///   <li>an approval if <code>Approval == true</code>;</li>
///   <li>a rejection if <code>Approval == false</code>; and</li>
///   <li>a comment if <code>Approval == null</code>.</li>
/// </ol>
/// Judgements are NOT created if:
/// <ul>
///   <li>its related run has "CREATED" status;</li>
///   <li>its <code>Note</code> is empty while its <code>Approved</code> is <code>false</code> or <code>null</code>.</li>
/// </ul>
/// I.e. for the second point, a mod MUST add a note if they want to reject or simply comment on a submission.
/// Moderators CANNOT modify their judgements once made.
/// </remarks>
public class Judgement
{
	/// <summary>Generated on creation.</summary>
	public long Id { get; set; }

	/// <summary>Defines this judgement, which in turn defines the status of its related run.</summary>
	/// <remarks>
	/// If:
	/// <ul>
	///   <li><code>true</code>, run is approved;</li>
	///   <li><code>false</code>, run is rejected;</li>
	///   <li><code>null</code>, run is commented on.</li>
	/// </ul>
	/// For the latter two, <code>Note</code> MUST be non-empty.
	/// </remarks>
	public bool? Approved { get; set; }

	/// <summary>When the judgement was made.</summary>
	[Required]
	public DateTime CreatedAt { get; set; }

	/// <summary>Comments on the judgement.</summary>
	/// <remarks>MUST be non-empty for rejections or comments (<code>Approved ∈ {false, null}</code>).</remarks>
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
