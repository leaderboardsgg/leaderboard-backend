using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Models
{
    public class LeaderboardContext : DbContext
    {
        public LeaderboardContext(DbContextOptions<LeaderboardContext> options)
            : base(options)
        {
        }

        public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
    }
}
