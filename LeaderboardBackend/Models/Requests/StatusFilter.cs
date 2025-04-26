using System.ComponentModel;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.Requests;

public enum StatusFilter
{
    Published,
    Deleted,
    Any
}

public static class StatusFilterMethods
{
    public static IQueryable<T> FilterByStatus<T>(this IQueryable<T> queryable, StatusFilter statusFilter) where T : IHasDeletionTimestamp
        => statusFilter switch
        {
            StatusFilter.Any => queryable,
            StatusFilter.Published => queryable.Where(ent => ent.DeletedAt == null),
            StatusFilter.Deleted => queryable.Where(ent => ent.DeletedAt != null),
            _ => throw new InvalidEnumArgumentException(nameof(statusFilter), (int)statusFilter, typeof(StatusFilter))
        };

    public static IEnumerable<T> FilterByStatus<T>(this IEnumerable<T> enumerable, StatusFilter statusFilter) where T : IHasDeletionTimestamp
        => enumerable.Where(ent => statusFilter == StatusFilter.Any || ((int)statusFilter) == (int)ent.Status);
}
