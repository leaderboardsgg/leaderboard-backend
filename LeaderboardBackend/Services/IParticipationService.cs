using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IParticipationService
{
    Task<Participation?> GetParticipation(long id);
    Task<Participation?> GetParticipationForUser(Guid userId);
    Task CreateParticipation(Participation participation);
    Task UpdateParticipation(Participation participation);
    Task<List<Participation>> GetParticipationsForRun(Run run);
}
