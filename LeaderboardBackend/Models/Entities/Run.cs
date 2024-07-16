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
    ///     The time the run was created.
    /// </summary>
    [Required]
    public Instant CreatedAt { get; set; }

    /// <summary>
    ///     The last time the run was updated or <see langword="null" />.
    /// </summary>
    public Instant? UpdatedAt { get; set; }

    /// <summary>
    ///     The time at which the run was deleted, or <see langword="null" /> if the run has not been deleted.
    /// </summary>
    public Instant? DeletedAt { get; set; }

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
