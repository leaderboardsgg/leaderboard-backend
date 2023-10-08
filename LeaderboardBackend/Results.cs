namespace LeaderboardBackend.Result;

public readonly record struct AccountConfirmed();
public readonly record struct AlreadyUsed();
public readonly record struct BadCredentials();
public readonly record struct BadRole();
public readonly record struct ConfirmationNotFound();
public readonly record struct EmailFailed();
public readonly record struct Expired();
public readonly record struct Old();
public readonly record struct UserNotFound();
public readonly record struct UserBanned();
