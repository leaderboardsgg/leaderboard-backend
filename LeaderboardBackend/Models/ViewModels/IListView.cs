namespace LeaderboardBackend.Models.ViewModels;

public interface IListView
{
    long Total { get; set; }
    int LimitDefault { get; set; }
    int LimitMax { get; set; }
}
