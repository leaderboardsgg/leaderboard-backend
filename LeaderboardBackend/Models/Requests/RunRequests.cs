using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using NodaTime;

#nullable disable warnings

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     Request sent when creating a Run. Set `runType` to `"Time"` for a timed
///     request, and `"Score"` for a scored one. `runType` *must* be at the top
///     of the request object.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "runType")]
[JsonDerivedType(typeof(CreateTimedRunRequest), nameof(RunType.Time))]
[JsonDerivedType(typeof(CreateScoredRunRequest), nameof(RunType.Score))]
public abstract class CreateRunRequest
{
    /// <inheritdoc cref="Entities.Run.Info" />
    public string Info { get; set; }

    /// <summary>
    ///     The date the `Run` was played on. Must obey the format 'YYYY-MM-DD', with leading zeroes.
    /// </summary>
    /// <example>2025-01-01</example>
    [Required]
    public required LocalDate PlayedOn { get; set; }
}

/// <summary>
///     `runType: "Time"`
/// </summary>
public class CreateTimedRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The duration of the run. Must obey the format 'HH:mm:ss.sss', with leading zeroes.
    /// </summary>
    /// <example>12:34:56.999</example>
    [Required]
    public required Duration Time { get; set; }
}

/// <summary>
///     `runType: "Score"`
/// </summary>
public class CreateScoredRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The score achieved during the run.
    /// </summary>
    [Required]
    public required long Score { get; set; }
}

public class CreateRunRequestValidator : AbstractValidator<CreateRunRequest>
{
    public CreateRunRequestValidator(IClock clock) =>
        RuleFor(x => x.PlayedOn)
            .LessThanOrEqualTo(date => clock.GetCurrentInstant().InUtc().Date)
            .WithMessage("{PropertyName} must not be set in the future.");
}

public class CreateTimedRunRequestValidator : AbstractValidator<CreateTimedRunRequest>
{
    public CreateTimedRunRequestValidator(IClock clock)
    {
        Include(new CreateRunRequestValidator(clock));
        RuleFor(x => x.Time)
            .GreaterThanOrEqualTo(Duration.Zero)
            .WithMessage("{PropertyName} must be positive.");
    }
}

public class CreateScoredRunRequestValidator : AbstractValidator<CreateScoredRunRequest>
{
    public CreateScoredRunRequestValidator(IClock clock) => Include(new CreateRunRequestValidator(clock));
}
