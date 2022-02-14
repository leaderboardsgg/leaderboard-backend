using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IParticipationService
	{
		Task<Participation?> GetParticipation(long id);
		Task CreateParticipation(Participation participation);
	}
}
