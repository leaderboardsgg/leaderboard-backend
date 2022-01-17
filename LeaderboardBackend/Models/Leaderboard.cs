using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models
{
    public class Leaderboard
    {
        public long Id { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Slug { get; set; }

        [Required]
        public string? Rules { get; set; }
    }
}
