namespace Adappt.Azure.B2C.Security.Utils;

public static class B2CConnector
{
    public const string Version = "1.0.0";
    public const string BlockingResponseMessage =
        "There was a problem with your request. You are not able to sign up at this time. Please contact your system administrator";

    public enum Action
    {
        ShowBlockPage,
        Continue,
        ValidationError,
    }
}
