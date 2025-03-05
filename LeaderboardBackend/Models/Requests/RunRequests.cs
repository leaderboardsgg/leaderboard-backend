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
    ///     The date the `Run` was played on. Must obey the format 'YYYY-MM-DD', with leading zeroes.
    /// </summary>
    /// <example>2025-01-01</example>
    public required LocalDate PlayedOn { get; set; }
}

public record CreateTimedRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The duration of the run. Must obey the format 'HH:mm:ss.sss', with leading zeroes.
    /// </summary>
    /// <example>00:12:34:56.999</example>
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
    // TODO: This validator fails to trigger. We're able to create runs even when
    // PlayedOn is far into the future.
    public CreateRunRequestValidator(IClock clock) =>
        RuleFor(x => x.PlayedOn).Must(date =>
        {
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
    public CreateScoredRunRequestValidator(IClock clock) => Include(new CreateRunRequestValidator(clock));
}
