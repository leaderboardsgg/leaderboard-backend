using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a decision made by a *Moderator* (`User`) about a `Run`.
/// </summary>
/// <remarks>
///     The latest Judgement on a Run updates its status.<br/>
///     A Judgement may be one of these types:<br/>
///         - an approval (when Approval is true);<br/>
///         - a rejection (when Approval is false);<br/>
///         - a comment (when Approval is null).<br/>
///     A Judgement is not created when:<br/>
///         - the related Run's status is CREATED;<br/>
///         - its Note is empty while Approved is null or false.<br/>
///     A Judgement may not be modified once created.
/// </remarks>
public class Judgement
{
    /// <summary>
    ///     The unique identifier of the `Judgement`.<br/>
    ///     Generated on creation.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///     The time the `Judgement` was made.<br/>
    ///     Generated on creation.
    /// </summary>
    [Required]
    public Instant CreatedAt { get; set; }

    /// <summary>
    ///     The ID of the `Run` which is being judged.
    /// </summary>
    [Required]
    public Guid RunId { get; set; }

    /// <summary>
    ///     Relationship model for `RunId`.
    /// </summary>
    [Required]
    public Run Run { get; set; } = null!;

    /// <summary>
    ///     The ID of the *Moderator* (`User`) who is making the `Judgement`.
    /// </summary>
    [Required]
    public Guid JudgeId { get; set; }

    /// <summary>
    ///     Relationship model for `JudgeId`.
    /// </summary>
    [Required]
    public User Judge { get; set; } = null!;

    /// <summary>
    ///     The `Judgement`'s decision. May be null, true, or false.
    /// </summary>
    public bool? Approved { get; set; }

    /// <summary>
    ///     A comment elaborating on the `Judgement`'s decision. Must have a value when the
    ///     affected `Run` is not approved (`Approved` is null or false).
    /// </summary>
    /// <example>The video proof is not of sufficient quality.</example>
    [Required]
    public string Note { get; set; } = "";
}
