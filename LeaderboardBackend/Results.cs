using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Result;

public readonly record struct AccountConfirmed;
public readonly record struct AlreadyDeleted;
public readonly record struct AlreadyUsed;
public readonly record struct BadCredentials;
public readonly record struct BadRole;
public readonly record struct ConfirmationNotFound;
public record Conflict<T>(T Conflicting);
public readonly record struct EmailFailed;
public readonly record struct Expired;
public readonly record struct NeverDeleted;
public readonly record struct BadRunType;
public readonly record struct UserNotFound;
public readonly record struct UserBanned;

[GenerateOneOf]
public partial class DeleteResult : OneOfBase<Success, NotFound, AlreadyDeleted>;

[GenerateOneOf]
public partial class UpdateResult<T> : OneOfBase<Conflict<T>, NotFound, Success>;

[GenerateOneOf]
public partial class RestoreResult<T> : OneOfBase<T, NotFound, NeverDeleted, Conflict<T>>;
