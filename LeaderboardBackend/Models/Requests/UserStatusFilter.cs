using System.ComponentModel;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.Requests;
public enum UserStatusFilter
{
    NotBanned,
    Banned,
    All
}

public static class UserStatusFilterMethods
{
    public static IQueryable<User> FilterByUserStatus(this IQueryable<User> queryable, UserStatusFilter statusFilter)
        => statusFilter switch
        {
            UserStatusFilter.All => queryable,
            UserStatusFilter.Banned => queryable.Where(usr => usr.Role == UserRole.Banned),
            UserStatusFilter.NotBanned => queryable.Where(usr => usr.Role != UserRole.Banned),
            _ => throw new InvalidEnumArgumentException(nameof(statusFilter), (int)statusFilter, typeof(StatusFilter))
        };

    public static IEnumerable<User> FilterByUserStatus(this IEnumerable<User> enumerable, UserStatusFilter statusFilter)
        => enumerable.Where(usr => statusFilter == UserStatusFilter.All || ((int)statusFilter) == (int)usr.Status());
}
