using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class ParticipationService : IParticipationService
{
	private ApplicationContext ApplicationContext;
	public ParticipationService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Participation?> GetParticipation(long id)
	{
		Participation? participation = await ApplicationContext.Participations.FindAsync(id);
		return participation;
	}

	public async Task<Participation?> GetParticipationForUser(Guid userId)
	{
		return await ApplicationContext.Participations.SingleOrDefaultAsync(p => p.RunnerId == userId);
	}

	public async Task CreateParticipation(Participation participation)
	{
		ApplicationContext.Participations.Add(participation);
		await ApplicationContext.SaveChangesAsync();
	}

	public async Task UpdateParticipation(Participation participation)
	{
		ApplicationContext.Participations.Update(participation);
		await ApplicationContext.SaveChangesAsync();
	}

	public async Task<List<Participation>> GetParticipationsForRun(Run run)
	{
		return await ApplicationContext.Participations.Where(p => p.RunId == run.Id).ToListAsync();
	}
}
