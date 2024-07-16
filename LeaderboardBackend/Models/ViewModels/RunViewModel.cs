using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.ViewModels;

public record RunViewModel
{
    /// <summary>
    ///     The unique identifier of the `Run`.<br/>
    ///     Generated on creation.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    ///     The time the run was created.
    /// </summary>
    public required Instant CreatedAt { get; set; }

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
    public required long CategoryId { get; set; }

    public static RunViewModel MapFrom(Run run) => new()
    {
        Id = run.Id,
        CategoryId = run.CategoryId,
        CreatedAt = run.CreatedAt,
        UpdatedAt = run.UpdatedAt,
        DeletedAt = run.DeletedAt
    };
}

