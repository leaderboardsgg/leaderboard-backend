using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents an entry on a `Category`.
/// </summary>
public class Run
{
    /// <summary>
    ///     The unique identifier of the `Run`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     The date the `Run` was played on.
    /// </summary>
    [Required]
    public LocalDate PlayedOn { get; set; }

    /// <summary>
    ///     The time the request was made at.
    /// </summary>
    [Required]
    public Instant SubmittedAt { get; set; }

    /// <summary>
    /// 	The ID of the `Category` for `Run`.
    /// </summary>
    [Required]
    public long CategoryId { get; set; }

    /// <summary>
    /// 	Relationship model for `CategoryId`.
    /// </summary>
    public Category? Category { get; set; }
}
