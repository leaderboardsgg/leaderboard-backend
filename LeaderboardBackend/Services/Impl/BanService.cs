using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Services;

public class BanService : IBanService
{
	private readonly ApplicationContext _applicationContext;
	private readonly IConfiguration _config;

	public BanService(ApplicationContext applicationContext, IConfiguration config)
	{
		_applicationContext = applicationContext;
		_config = config;
	}

	public async Task<Ban?> GetBanById(long id)
	{
		return await _applicationContext.Bans
			.FindAsync(id);
	}

	public async Task<List<Ban>> GetBans()
	{
		return await _applicationContext.Bans
			.ToListAsync();
	}

	public async Task<List<Ban>> GetBansByLeaderboard(long leaderboardId)
	{
		return await _applicationContext.Bans
			.Where(ban => ban.LeaderboardId == leaderboardId)
			.ToListAsync();
	}

	public async Task<List<Ban>> GetBansByUser(Guid userId)
	{
		return await _applicationContext.Bans
			.Where(ban => ban.BannedUserId == userId)
			.ToListAsync();
	}

	public async Task CreateBan(Ban ban)
	{
		_applicationContext.Bans.Add(ban);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task DeleteBan(long id)
	{
		Ban ban = await _applicationContext.Bans
			.Where(ban => ban.Id == id)
			.FirstAsync();

		ban.DeletedAt = SystemClock.Instance.GetCurrentInstant();

		await _applicationContext.SaveChangesAsync();
	}

	public async Task DeleteLeaderboardBan(long id, long leaderboardId)
	{
		Ban ban = await _applicationContext.Bans
			.Where(ban => ban.Id == id)
			.Where(ban => ban.LeaderboardId == leaderboardId)
			.FirstAsync();

		ban.DeletedAt = SystemClock.Instance.GetCurrentInstant();

		await _applicationContext.SaveChangesAsync();
	}
}
