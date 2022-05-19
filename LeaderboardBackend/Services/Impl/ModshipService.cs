using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class ModshipService : IModshipService
{
	private readonly ApplicationContext _applicationContext;

	public ModshipService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Modship?> GetModship(Guid userId)
	{
		return await _applicationContext.Modships.FirstOrDefaultAsync(m => m.UserId == userId);
	}

	public async Task CreateModship(Modship modship)
	{
		_applicationContext.Modships.Add(modship);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task<List<Modship>> LoadUserModships(Guid userId)
	{
		return await _applicationContext.Modships
					.Where(m => m.UserId == userId)
					.ToListAsync();
	}

	public async Task DeleteModship(Modship modship)
	{
		var deleteModship = _applicationContext.Modships.Where(m =>
			m.LeaderboardId == modship.LeaderboardId &&
			m.UserId == modship.UserId)
			.FirstOrDefault() ?? throw new NullReferenceException();

		_applicationContext.Entry(deleteModship).State = EntityState.Deleted;
		await _applicationContext.SaveChangesAsync();
	}
}
