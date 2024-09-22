using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public interface IHasUpdateTimestamp : IHasCreationTimestamp
{
    Instant? UpdatedAt { get; set; }
}
