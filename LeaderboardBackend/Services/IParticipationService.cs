using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IParticipationService
{
	Task<Participation?> GetParticipation(long id);
	Task<Participation?> GetParticipationForUser(User user);
	Task CreateParticipation(Participation participation);
	Task UpdateParticipation(Participation participation);
}
