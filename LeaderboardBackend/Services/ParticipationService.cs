using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public class ParticipationService : IParticipationService
	{
		private ApplicationContext _applicationContext;
		public ParticipationService(ApplicationContext applicationContext)
		{
			_applicationContext = applicationContext;
		}

		public async Task<Participation?> GetParticipation(long id)
		{
			Participation? participation = await _applicationContext.Participations.FindAsync(id);
			return participation;
		}

		public async Task CreateParticipation(Participation participation)
		{
			_applicationContext.Participations.Add(participation);
			await _applicationContext.SaveChangesAsync();
		}
	}
}
