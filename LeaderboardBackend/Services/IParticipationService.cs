using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IParticipationService
	{
		Task<Participation?> GetParticipation(long id);
		Task<Participation?> GetParticipationForUser(User user);
		Task CreateParticipation(Participation participation);
	}
}
