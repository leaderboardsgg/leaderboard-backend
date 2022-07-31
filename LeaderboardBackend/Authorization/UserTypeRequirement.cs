using Microsoft.AspNetCore.Authorization;

namespace LeaderboardBackend.Authorization;

public record UserTypeRequirement : IAuthorizationRequirement
{
	public UserTypeRequirement(string type)
	{
		Type = type;
	}

	public string Type { get; }
}
