using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services
{
	public class BanService : IBanService
	{
		private ApplicationContext _applicationContext;
		private IConfiguration _config;
		public BanService(ApplicationContext applicationContext, IConfiguration config)
		{
			_applicationContext = applicationContext;
			_config = config;
		}

		public async Task<Ban?> GetBanById(ulong id)
		{
			return await _applicationContext.Bans.FindAsync(id);
		}

		public async Task<List<Ban>> GetBans(object? filter = null)
		{
			if (filter == null)
			{
				return await _applicationContext.Bans.ToListAsync();
			}
			if (filter.GetType() == typeof(ulong))
			{
				return await _applicationContext.Bans.Where(
					b => b.LeaderboardId != null && b.LeaderboardId == (ulong)filter
				).ToListAsync();
			}
			if (filter is Guid)
			{
				return await _applicationContext.Bans.Where(
					b => b.BannedUserId != null && b.BannedUserId == (Guid)filter
				).ToListAsync();
			}
			return new List<Ban>();
		}

		public async Task CreateBan(Ban ban)
		{
			_applicationContext.Bans.Add(ban);
			await _applicationContext.SaveChangesAsync();
		}
	}
}
