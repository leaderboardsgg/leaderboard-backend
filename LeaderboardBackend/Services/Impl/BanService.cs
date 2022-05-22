using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class BanService : IBanService
{
	private ApplicationContext ApplicationContext;
	private IConfiguration Config;
	public BanService(ApplicationContext applicationContext, IConfiguration config)
	{
		ApplicationContext = applicationContext;
		Config = config;
	}

	public async Task<Ban?> GetBanById(ulong id)
	{
		return await ApplicationContext.Bans.FindAsync(id);
	}

	public async Task<List<Ban>> GetBans(object? filter = null)
	{
		if (filter is null)
		{
			return await ApplicationContext.Bans.ToListAsync();
		}
		if (filter.GetType() == typeof(ulong))
		{
			return await ApplicationContext.Bans.Where(
				b => b.LeaderboardId != null && b.LeaderboardId == (long)filter
			).ToListAsync();
		}
		if (filter is Guid)
		{
			return await ApplicationContext.Bans.Where(
				b => b.BannedUserId == (Guid)filter
			).ToListAsync();
		}
		return new List<Ban>();
	}

	public async Task CreateBan(Ban ban)
	{
		ApplicationContext.Bans.Add(ban);
		await ApplicationContext.SaveChangesAsync();
	}
}
