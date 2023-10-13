using System;

namespace LeaderboardBackend.Test;

internal static class Routes
{
    public const string LOGIN = "/login";
    public const string REGISTER = "/account/register";
    public const string RESEND_CONFIRMATION = "/account/confirm";
    public const string RECOVER_ACCOUNT = "/account/recover";
    public static string ConfirmAccount(string id) => $"/account/confirm/{id}";
    public static string ConfirmAccount(Guid id) => ConfirmAccount(id.ToUrlSafeBase64String());
    public static string RecoverAccount(string id) => $"/account/recover/{id}";
    public static string RecoverAccount(Guid id) => RecoverAccount(id.ToUrlSafeBase64String());
}
