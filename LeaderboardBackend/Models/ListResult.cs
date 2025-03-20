namespace LeaderboardBackend.Models;

public record ListResult<T>
{
    public required List<T> Items { get; set; }
    public required long ItemsTotal { get; set; }
}
