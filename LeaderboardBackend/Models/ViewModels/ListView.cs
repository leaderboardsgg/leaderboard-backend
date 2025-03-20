namespace LeaderboardBackend.Models.ViewModels;

public record ListView<T>
{
    public required IList<T> Data { get; set; }
    public required long ItemsTotal { get; set; }
    public required int LimitDefault { get; set; }
    public required int LimitMax { get; set; }
}
