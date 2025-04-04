using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.ViewModels;

public record ListView<T> : IListView
{
    public required IList<T> Data { get; set; }

    /// <summary>
    ///     The total number of records matching the given criteria that
    ///     exist in the database, NOT the total number of records returned.
    /// </summary>
    public required long Total { get; set; }

    /// <summary>
    ///     The default limit that will be applied for this resource type
    ///     if the client does not specify one in the query string.
    /// </summary>
    /// <remarks>
    ///     This property will be set automatically when this object is returned in an object result
    ///     of a controller action annotated with <see cref="Filters.PaginatedAttribute"/>.
    /// </remarks>
    [Required]
    public int LimitDefault { get; set; }

    /// <summary>
    ///     The maximum value the client is allowed to specify as a limt for
    ///     endpoints return a paginated list of resources of this type.
    ///     Exceeding this value will result in an error.
    /// </summary>
    /// <remarks>
    ///     This property will be set automatically when this object is returned in an object result
    ///     of a controller action annotated with <see cref="Filters.PaginatedAttribute"/>.
    /// </remarks>
    [Required]
    public int LimitMax { get; set; }
}
