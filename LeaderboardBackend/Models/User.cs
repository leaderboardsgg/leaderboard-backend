using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models
{
    public class User
    {
        public long Id { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}