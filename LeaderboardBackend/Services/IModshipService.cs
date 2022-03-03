using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services;

public interface IModshipService
{
	Task<Modship?> GetModship(Guid userId);
	Task CreateModship(Modship modship);
	// TODO: Implement this
	// Task DeleteModship(Modship modship);
}
