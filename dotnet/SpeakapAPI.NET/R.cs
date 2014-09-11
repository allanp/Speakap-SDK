namespace Speakap.SDK
{
    /// <summary>
    /// 
    /// </summary>
    internal static class R
    {
        /// <summary>
        /// The default window a request is valid, in seconds
        /// </summary>
        internal const int DefaultSignatureWindowSize = 60; // seconds

        internal static string ISO8601DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff+0000";

        /// <summary>
        /// Should always be '&'
        /// </summary>
        internal const string Separator = "&";

        internal const string AppData = "appData";
        internal const string IssuedAt = "issuedAt";
        internal const string Locale = "locale";
        internal const string NetworkEID = "networkEID";
        internal const string Role = "role";
        internal const string UserEID = "userEID";
        internal const string Signature = "signature";

        internal static readonly string[] DefaultKeys = new[] { AppData, IssuedAt, Locale, NetworkEID, Role, UserEID, Signature };
    }
}