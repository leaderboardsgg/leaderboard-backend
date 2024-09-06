using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public interface IHasCreationTimestamp
{
    Instant CreatedAt { get; set; }
}
