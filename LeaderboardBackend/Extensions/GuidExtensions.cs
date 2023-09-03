namespace LeaderboardBackend;

public static class GuidExtensions
{
    public static string ToUrlSafeBase64String(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray(), Base64FormattingOptions.None).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static Guid FromUrlSafeBase64String(string s)
    {
        return new(Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + "=="));
    }
}
