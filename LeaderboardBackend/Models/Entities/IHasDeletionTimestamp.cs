using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public interface IHasDeletionTimestamp : IHasCreationTimestamp
{
    Instant? DeletedAt { get; set; }

    public Status Status => DeletedAt == null ? Status.Published : Status.Deleted;
}

public static class HasDeletionTimestamp
{
    public static Status Status(this IHasDeletionTimestamp entity) => entity.Status;
}
