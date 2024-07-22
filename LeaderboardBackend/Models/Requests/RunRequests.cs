using NodaTime;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Run`.
/// </summary>
public record CreateRunRequest
{
    /// <inheritdoc cref="Entities.Run.Info" />
    public required string? Info { get; set; }

    /// <summary>
    ///     The date the `Run` was played on.
    /// </summary>
    public required LocalDate PlayedOn { get; set; }

    /// <summary>
    /// 	The ID of the `Category` for the `Run`.
    /// </summary>
    public required long CategoryId { get; set; }

    /// <inheritdoc cref="Entities.Run.TimeOrScore" />
    public required long TimeOrScore { get; set; }
}
