using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Services;

public class ParticipationService : IParticipationService
{
	private readonly ApplicationContext _applicationContext;
	public ParticipationService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Participation?> GetParticipation(long id)
	{
		Participation? participation = await _applicationContext.Participations
			.FindAsync(id);

		return participation;
	}

	public async Task<Participation?> GetParticipationForUser(Guid userId)
	{
		return await _applicationContext.Participations
			.SingleOrDefaultAsync(participation => participation.RunnerId == userId);
	}

	public async Task CreateParticipation(Participation participation)
	{
		_applicationContext.Participations.Add(participation);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task UpdateParticipation(Participation participation)
	{
		_applicationContext.Participations.Update(participation);
		participation.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
		await _applicationContext.SaveChangesAsync();
	}

	public async Task<List<Participation>> GetParticipationsForRun(Run run)
	{
		return await _applicationContext.Participations
			.Where(participation => participation.RunId == run.Id)
			.ToListAsync();
	}
}
