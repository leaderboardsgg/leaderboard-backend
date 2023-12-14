namespace LeaderboardBackend;

public static class StringExtension
{
    public static string Obfuscate(this string str) =>
        str.Length < 3 ? $"{str[..1]}e***{str[^1..]}" : $"{str[..2]}***{str[^1..]}";
}
