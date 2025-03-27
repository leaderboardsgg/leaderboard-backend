namespace LeaderboardBackend.Models.ViewModels;

public record ListView<T>
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
    public required int LimitDefault { get; set; }

    /// <summary>
    ///     The maximum value the client is allowed to specify as a limt for
    ///     endpoints return a paginated list of resources of this type.
    ///     Exceeding this value will result in an error.
    /// </summary>
    public required int LimitMax { get; set; }
}
