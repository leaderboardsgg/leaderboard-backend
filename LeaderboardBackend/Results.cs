using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Result;

public readonly record struct AccountConfirmed;
/// <summary>
/// <param name="DeletedEntity">
/// The entity that is deleted. Useful for when an action on an entity can't be
/// done because a relation is deleted, and we want to point out the offending
/// entity in response to clients. For example: a Run can't be updated because
/// its Leaderboard is deleted, and we want to let clients know that.
/// </param>
/// </summary>
public readonly record struct AlreadyDeleted(Type? DeletedEntity = null);
public readonly record struct AlreadyUsed;
public readonly record struct BadCredentials;
public readonly record struct BadRole;
public readonly record struct ConfirmationNotFound;
public record Conflict<T>(T Conflicting);
public readonly record struct EmailFailed;
public readonly record struct Expired;
public readonly record struct ListResult<T>(List<T> Items, long ItemsTotal);
public readonly record struct NeverDeleted;
public readonly record struct BadRunType;
public readonly record struct UserNotFound;
public readonly record struct UserBanned;
public readonly record struct UserDoesNotOwnRun;

[GenerateOneOf]
public partial class DeleteResult : OneOfBase<Success, NotFound, AlreadyDeleted>;

[GenerateOneOf]
public partial class UpdateResult<T> : OneOfBase<Conflict<T>, NotFound, Success>;

[GenerateOneOf]
public partial class RestoreResult<T> : OneOfBase<T, NotFound, NeverDeleted, Conflict<T>>;
