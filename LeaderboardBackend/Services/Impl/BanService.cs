using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

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

	public async Task<Ban?> GetBanById(long id)
	{
		return await ApplicationContext.Bans.FindAsync(id);
	}

	public async Task<List<Ban>> GetBans()
	{
		return await ApplicationContext.Bans.ToListAsync();
	}

	public async Task<List<Ban>> GetBansByLeaderboard(long leaderboardId)
	{
		return await ApplicationContext.Bans.Where(
			b => b.LeaderboardId == leaderboardId
		).ToListAsync();
	}

	public async Task<List<Ban>> GetBansByUser(Guid userId)
	{
		return await ApplicationContext.Bans.Where(
			b => b.BannedUserId == userId
		).ToListAsync();
	}

	public async Task CreateBan(Ban ban)
	{
		ApplicationContext.Bans.Add(ban);
		await ApplicationContext.SaveChangesAsync();
	}

	public async Task DeleteBan(long id)
	{
		Ban ban = await ApplicationContext.Bans.Where(b => b.Id == id).FirstAsync();
		ban.DeletedAt = SystemClock.Instance.GetCurrentInstant();
		await ApplicationContext.SaveChangesAsync();
	}
}
