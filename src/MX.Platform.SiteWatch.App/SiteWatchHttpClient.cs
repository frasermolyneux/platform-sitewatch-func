using Microsoft.Extensions.Logging;

namespace MX.Platform.SiteWatch.App;

internal static class SiteWatchHttpClient
{
    public const string Name = "SiteWatch";
    public const string LoggingCategoryPrefix = $"System.Net.Http.HttpClient.{Name}.";

    public static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.AddFilter(LoggingCategoryPrefix, LogLevel.Warning);
    }
}
