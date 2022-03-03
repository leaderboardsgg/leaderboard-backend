using LeaderboardBackend.Models;
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
		return await _applicationContext.Modships.SingleOrDefaultAsync(m => m.UserId == userId);
	}

	public async Task CreateModship(Modship modship)
	{
		_applicationContext.Modships.Add(modship);
		await _applicationContext.SaveChangesAsync();
	}

	// TODO: Implement this
	// public async Task DeleteModship(Modship modship)
	// {
	// }
}
