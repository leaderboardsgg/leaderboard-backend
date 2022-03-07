using System.IdentityModel.Tokens.Jwt;

namespace LeaderboardBackend.Authorization;

/// <summary>
/// Class that holds a JwtSecurityTokenHandler instance.
/// </summary>
/// <remarks>
/// Singleton definition style taken from https://csharpindepth.com/Articles/Singleton.
/// </remarks>
public sealed class JwtSecurityTokenHandlerSingleton
{
	public static readonly JwtSecurityTokenHandler Instance = new();
	private JwtSecurityTokenHandlerSingleton() {}
}
