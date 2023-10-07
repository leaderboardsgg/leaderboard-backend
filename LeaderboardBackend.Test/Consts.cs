using System;

namespace LeaderboardBackend.Test;

internal static class Routes
{
    public const string LOGIN = "/login";
    public const string REGISTER = "/account/register";
    public const string RESEND_CONFIRMATION = "/account/confirm";
    public const string RECOVER_ACCOUNT = "/account/recover";
    public static string ConfirmAccount(Guid id) => $"/account/confirm/{id.ToUrlSafeBase64String()}";
    public static string RecoverAccount(Guid id) => $"/account/recover/{id.ToUrlSafeBase64String()}";
}
