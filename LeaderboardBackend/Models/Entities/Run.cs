using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents an entry on a `Category`.
/// </summary>
public class Run : IHasUpdateTimestamp
{
    /// <summary>
    ///     The unique identifier of the `Run`.<br/>
    ///     Generated on creation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     User-provided details about the run.
    /// </summary>
    public string? Info { get; set; }

    public RunType Type => Category.Type;

    [NotMapped]
    public Duration Time
    {
        get => Duration.FromNanoseconds(TimeOrScore);
        set => TimeOrScore = value.ToInt64Nanoseconds();
    }

    /// <summary>
    ///     The duration of the run in nanoseconds if the run belongs to a timed category, otherwise the score.
    /// </summary>
    public long TimeOrScore { get; set; }

    /// <summary>
    ///     The date the `Run` was played on.
    /// </summary>
    public LocalDate PlayedOn { get; set; }

    /// <summary>
    ///     The time the run was created.
    /// </summary>
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
    public long CategoryId { get; set; }

    /// <summary>
    /// 	Relationship model for `CategoryId`.
    /// </summary>
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }

    /// <summary>
    ///     The User who submitted the run.
    /// </summary>
    public User User { get; set; } = null!;
}
