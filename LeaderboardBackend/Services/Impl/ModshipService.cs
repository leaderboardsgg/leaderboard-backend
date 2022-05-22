using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class ModshipService : IModshipService
{
	private readonly ApplicationContext ApplicationContext;

	public ModshipService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Modship?> GetModship(Guid userId)
	{
		return await ApplicationContext.Modships.FirstOrDefaultAsync(m => m.UserId == userId);
	}

	public async Task<Modship?> GetModshipForLeaderboard(long leaderboardId, Guid userId)
	{
		return await ApplicationContext.Modships.SingleOrDefaultAsync(m =>
			m.LeaderboardId == leaderboardId &&
			m.UserId == userId);
	}

	public async Task CreateModship(Modship modship)
	{
		ApplicationContext.Modships.Add(modship);
		await ApplicationContext.SaveChangesAsync();
	}

	public async Task<List<Modship>> LoadUserModships(Guid userId)
	{
		return await ApplicationContext.Modships
					.Where(m => m.UserId == userId)
					.ToListAsync();
	}

	public async Task DeleteModship(Modship modship)
	{
		ApplicationContext.Entry(modship).State = EntityState.Deleted;
		await ApplicationContext.SaveChangesAsync();
	}
}
