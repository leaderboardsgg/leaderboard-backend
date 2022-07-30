using System.IdentityModel.Tokens.Jwt;

namespace LeaderboardBackend.Authorization;

/// <summary>
///     Class that holds a JwtSecurityTokenHandler instance.
/// </summary>
/// <remarks>
///     Singleton definition style taken from https://csharpindepth.com/Articles/Singleton.
/// </remarks>
public static class JwtSecurityTokenHandlerSingleton
{
	public static JwtSecurityTokenHandler Instance { get; } = new();
}
