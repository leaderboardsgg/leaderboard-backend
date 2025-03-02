using System.Text.Json.Serialization;
using FluentValidation;
using NodaTime;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Run`.
/// </summary>
[JsonDerivedType(typeof(CreateRunRequest), "Base")]
[JsonDerivedType(typeof(CreateTimedRunRequest), "Time")]
[JsonDerivedType(typeof(CreateScoredRunRequest), "Score")]
public record CreateRunRequest
{
    /// <inheritdoc cref="Entities.Run.Info" />
    public required string Info { get; set; }

    /// <summary>
    ///     The date the `Run` was played on.
    /// </summary>
    public required LocalDate PlayedOn { get; set; }
}

public record CreateTimedRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The duration of the run.
    /// </summary>
    public required Duration Time { get; set; }
}

public record CreateScoredRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The score achieved during the run.
    /// </summary>
    public required long Score { get; set; }
}

public class CreateRunRequestValidator : AbstractValidator<CreateRunRequest>
{
    public CreateRunRequestValidator(IClock clock) =>
        RuleFor(x => x.PlayedOn).Must(date => {
            // TODO: This is likely wrong. Will need to convert timezones.
            LocalDate today = clock.GetCurrentInstant().InUtc().Date;
            return date <= today;
        });
}

public class CreateTimedRunRequestValidator : AbstractValidator<CreateTimedRunRequest>
{
    public CreateTimedRunRequestValidator(IClock clock)
    {
        Include(new CreateRunRequestValidator(clock));
        // TODO: There must be a better way than using a regex.
        RuleFor(x => x.Time).GreaterThanOrEqualTo(Duration.Zero);
    }
}

public class CreateScoredRunRequestValidator : AbstractValidator<CreateScoredRunRequest>
{
    public CreateScoredRunRequestValidator(IClock clock)
    {
        Include(new CreateRunRequestValidator(clock));
        RuleFor(x => x.Score).GreaterThan(0);
    }
}
