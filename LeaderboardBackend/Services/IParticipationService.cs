using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IParticipationService
	{
		Task<Participation?> GetParticipation(long id);
	}
}
