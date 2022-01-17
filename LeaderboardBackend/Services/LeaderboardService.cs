using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private LeaderboardContext _leaderboardContext;
        private IConfiguration _config;
        public LeaderboardService(LeaderboardContext leaderboardContext, IConfiguration config)
        {
            _leaderboardContext = leaderboardContext;
            _config = config;
        }

        public async Task<Leaderboard?> GetLeaderboard(long id)
        {
            Leaderboard? leaderboard = await _leaderboardContext.Leaderboards.FindAsync(id);
            return leaderboard;
        }

        public async Task<List<Leaderboard[]>> GetLeaderboards(long[] ?ids = null)
        {
            if (ids == null) {
                return await _leaderboardContext.Leaderboards.Chunk(20).ToListAsync();
            } else {
                return await _leaderboardContext.Leaderboards.Where(l => ids.Contains(l.Id)).Chunk(20).ToListAsync();
            }
        }

        public async Task CreateLeaderboard(Leaderboard leaderboard)
        {
            _leaderboardContext.Leaderboards.Add(leaderboard);
            await _leaderboardContext.SaveChangesAsync();
        }
    }
}
