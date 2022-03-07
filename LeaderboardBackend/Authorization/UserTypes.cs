namespace LeaderboardBackend.Authorization;

public readonly record struct UserTypes
{
	public const string Admin = "Admin";
	public const string Mod = "Mod";
	public const string User = "User";
}
