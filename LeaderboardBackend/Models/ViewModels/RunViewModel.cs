using System.Text.Json.Serialization;
using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.ViewModels;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "runType")]
[JsonDerivedType(typeof(TimedRunViewModel), "Time")]
[JsonDerivedType(typeof(ScoredRunViewModel), "Score")]
public abstract record RunViewModel
{
    /// <summary>
    ///     The unique identifier of the `Run`.<br/>
    ///     Generated on creation.
    /// </summary>
    public required Guid Id { get; set; }

    public required RunType RunType { get; set; }

    /// <summary>
    ///     User-provided details about the run.
    /// </summary>
    public required string? Info { get; set; }

    /// <summary>
    ///     The date the run was done, *not* when it was submitted.
    /// </summary>
    public required LocalDate PlayedOn { get; set; }

    /// <summary>
    ///     The time the run was submitted to the DB.
    /// </summary>
    public required Instant CreatedAt { get; set; }

    /// <summary>
    ///     The last time the run was updated or <see langword="null" />.
    /// </summary>
    public required Instant? UpdatedAt { get; set; }

    /// <summary>
    ///     The time at which the run was deleted, or <see langword="null" /> if the run has not been deleted.
    /// </summary>
    public required Instant? DeletedAt { get; set; }

    /// <summary>
    /// 	The ID of the `Category` for `Run`.
    /// </summary>
    public required long CategoryId { get; set; }

    /// <summary>
    ///     The user who submitted this run.
    /// </summary>
    public required UserViewModel User { get; set; }

    public required Status Status { get; set; }

    /// <summary>
    /// The run's rank within its category.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long Rank { get; set; }

    public static RunViewModel MapFrom(Run run) => run.Type switch
    {
        RunType.Time => new TimedRunViewModel
        {
            Id = run.Id,
            CategoryId = run.CategoryId,
            User = UserViewModel.MapFrom(run.User),
            PlayedOn = run.PlayedOn,
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt,
            DeletedAt = run.DeletedAt,
            Info = run.Info,
            Time = run.Time,
            RunType = run.Type,
            Status = run.Status()
        },
        RunType.Score => new ScoredRunViewModel
        {
            Id = run.Id,
            CategoryId = run.CategoryId,
            User = UserViewModel.MapFrom(run.User),
            PlayedOn = run.PlayedOn,
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt,
            DeletedAt = run.DeletedAt,
            Info = run.Info,
            Score = run.TimeOrScore,
            RunType = run.Type,
            Status = run.Status()
        },
        _ => throw new NotImplementedException(),
    };

    public static RunViewModel MapFrom(RankedRun ranked)
    {
        RunViewModel viewModel = MapFrom(ranked.Run);
        viewModel.Rank = ranked.Rank;
        return viewModel;
    }
}

public record TimedRunViewModel : RunViewModel
{
    /// <summary>
    ///     The duration of the run.
    /// </summary>
    public required Duration Time { get; set; }
}

public record ScoredRunViewModel : RunViewModel
{
    /// <summary>
    ///     The score achieved during the run.
    /// </summary>
    public required long Score { get; set; }
}
