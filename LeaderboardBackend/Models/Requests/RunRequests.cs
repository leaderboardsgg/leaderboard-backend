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
public abstract record CreateRunRequest
{
    [Required]
    public RunType RunType { get; set; }

    /// <inheritdoc cref="Entities.Run.Info" />
    public string Info { get; set; }

    /// <summary>
    ///     The date the `Run` was played on. Must obey the format 'YYYY-MM-DD', with leading zeroes.
    /// </summary>
    [Required]
    public LocalDate PlayedOn { get; set; }
}

/// <summary>
///     `runType: "Time"`
/// </summary>
public record CreateTimedRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The duration of the run. Must obey the format 'HH:mm:ss.sss', with leading zeroes.
    /// </summary>
    /// <example>12:34:56.999</example>
    [Required]
    public Duration Time { get; set; }
}

/// <summary>
///     `runType: "Score"`
/// </summary>
public record CreateScoredRunRequest : CreateRunRequest
{
    /// <summary>
    ///     The score achieved during the run.
    /// </summary>
    [Required]
    public long Score { get; set; }
}

/// <summary>
///     Request sent when updating a run.
///     All fields are optional but you must specify at least one.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "runType")]
[JsonDerivedType(typeof(UpdateTimedRunRequest), nameof(RunType.Time))]
[JsonDerivedType(typeof(UpdateScoredRunRequest), nameof(RunType.Score))]
public abstract record UpdateRunRequest
{
    [Required]
    public RunType RunType { get; set; }

    /// <inheritdoc cref="Entities.Run.Info" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Info { get; set; }

    /// <inheritdoc cref="CreateRunRequest.PlayedOn" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LocalDate? PlayedOn { get; set; }
}

public record UpdateTimedRunRequest : UpdateRunRequest
{
    /// <inheritdoc cref="CreateTimedRunRequest.Time" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Duration? Time { get; set; }
}

public record UpdateScoredRunRequest : UpdateRunRequest
{
    /// <inheritdoc cref="CreateScoredRunRequest.Score" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Score { get; set; }
}

public class UpdateRunRequestValidator : AbstractValidator<UpdateRunRequest>
{
    public UpdateRunRequestValidator() =>
        RuleFor(x => x).SetInheritanceValidator(v =>
            v.Add<UpdateTimedRunRequest>(new UpdateTimedRunRequestValidator())
            .Add<UpdateScoredRunRequest>(new UpdateScoredRunRequestValidator())
        );
}

public class UpdateTimedRunRequestValidator : AbstractValidator<UpdateTimedRunRequest>
{
    public UpdateTimedRunRequestValidator() =>
        RuleFor(x => x).Must(
            utrr => utrr.Info is not null ||
            utrr.PlayedOn is not null ||
            utrr.Time is not null);
}

public class UpdateScoredRunRequestValidator : AbstractValidator<UpdateScoredRunRequest>
{
    public UpdateScoredRunRequestValidator() =>
        RuleFor(x => x).Must(
            usrr => usrr.Info is not null ||
            usrr.PlayedOn is not null ||
            usrr.Score is not null);
}
