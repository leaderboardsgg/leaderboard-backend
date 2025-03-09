using System.Text.Json.Serialization;
using FluentValidation;
using NodaTime;
using OneOf;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     Request sent when creating a Run. This definition only shows fields
///     common across all Categories. Depending on the specific category, an
///     an extra field is expected:
///
///     * For timed Runs, a `time` field. It must have the format
///       'HH:mm:ss.sss' with leading zeroes.
///     * For scored Runs, a `score` field. It must be a number.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(CreateTimedRunRequest), "Time")]
[JsonDerivedType(typeof(CreateScoredRunRequest), "Score")]
public record CreateRunRequestBase
{
    /// <inheritdoc cref="Entities.Run.Info" />
    public required string Info { get; set; }

    /// <summary>
    ///     The date the `Run` was played on. Must obey the format 'YYYY-MM-DD', with leading zeroes.
    /// </summary>
    /// <example>2025-01-01</example>
    public required LocalDate PlayedOn { get; set; }
}

public record CreateTimedRunRequest : CreateRunRequestBase
{
    /// <summary>
    ///     The duration of the run. Must obey the format 'HH:mm:ss.sss', with leading zeroes.
    /// </summary>
    /// <example>00:12:34:56.999</example>
    public required Duration Time { get; set; }
}

public record CreateScoredRunRequest : CreateRunRequestBase
{
    /// <summary>
    ///     The score achieved during the run.
    /// </summary>
    public required long Score { get; set; }
}

[GenerateOneOf]
public partial class CreateRunRequest : OneOfBase<CreateTimedRunRequest, CreateScoredRunRequest>;

public class CreateRunRequestValidator : AbstractValidator<CreateRunRequestBase>
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
