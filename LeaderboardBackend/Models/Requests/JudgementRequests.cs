using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>Request object sent when creating a Judgement.</summary>
/// <param name="RunId">GUID of the run.</param>
/// <param name="Note">
///   Judgement comments. Must be provided if not outright approving a run (<paramref name="Approved"/> is false or null).
///   Acts as mod feedback for the runner.
/// </param>
/// <param name="Approved">
///   The judgement result. Can be true, false, or null. For the latter two, <paramref name="Note"/> must be non-empty.
/// </param>
public readonly record struct CreateJudgementRequest(Guid RunId, [Note] string Note, bool? Approved);
