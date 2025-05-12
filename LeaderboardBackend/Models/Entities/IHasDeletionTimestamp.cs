using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public interface IHasDeletionTimestamp : IHasCreationTimestamp
{
    Instant? DeletedAt { get; set; }
}

public static class HasDeletionTimestamp
{
    public static Status Status(this IHasDeletionTimestamp entity) => entity.DeletedAt is null ? Models.Status.Published : Models.Status.Deleted;
}
