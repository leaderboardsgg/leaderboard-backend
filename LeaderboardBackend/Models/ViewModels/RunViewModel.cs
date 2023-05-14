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
    ///     The time the request was made at.
    /// </summary>
    public required Instant SubmittedAt { get; set; }

    /// <summary>
    /// 	The ID of the `Category` for `Run`.
    /// </summary>
    public required long CategoryId { get; set; }

    public static RunViewModel MapFrom(Run run)
    {
        return new RunViewModel
        {
            Id = run.Id,
            CategoryId = run.CategoryId,
            SubmittedAt = run.SubmittedAt
        };
    }
}

