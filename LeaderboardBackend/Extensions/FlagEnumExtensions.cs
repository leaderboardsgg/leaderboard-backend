namespace LeaderboardBackend;

public static class FlagEnumExtensions
{
    /// <summary>
    ///     Return the flags that are present in <paramref name="flags"/>.
    /// </summary>
    /// <typeparam name="T">
    ///     The enum type. Must be a flag enum.
    ///     Cannot have any non-power-of-two members.
    /// </typeparam>
    public static IEnumerable<T> GetFlagValues<T>(this T flags) where T : struct, Enum =>
        Enum.GetValues<T>().Where(val => flags.HasFlag(val));
}
