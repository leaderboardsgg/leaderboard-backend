using Microsoft.AspNetCore.Authorization;

namespace LeaderboardBackend.Authorization;

public record UserTypeRequirement(string Type) : IAuthorizationRequirement;
