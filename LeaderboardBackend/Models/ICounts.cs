using System.Numerics;

namespace LeaderboardBackend.Models;

public interface ICounts<TBinInt> where TBinInt : IBinaryInteger<TBinInt>
{
    TBinInt Count { get; set; }
}
