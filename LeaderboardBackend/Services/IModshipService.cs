using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IModshipService
{
	Task<Modship?> GetModship(Guid userId);
	Task CreateModship(Modship modship);
	Task<List<Modship>> LoadUserModships(Guid userId);
	Task DeleteModship(Modship modship);
}
