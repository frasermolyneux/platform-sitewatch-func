namespace MX.Platform.SiteWatch.App;

public sealed class AvailabilitySchedule
{
    private readonly Lock sync = new();
    private readonly Dictionary<(string Site, string Check), DateTimeOffset> started = [];

    internal bool TrySchedule(TestConfig test, DateTimeOffset now)
    {
        if (test.IntervalSeconds == 30)
        {
            return true;
        }

        lock (sync)
        {
            var key = (test.Site, test.EffectiveCheckId);
            if (started.TryGetValue(key, out var last)
                && now >= last && now - last < TimeSpan.FromSeconds(test.IntervalSeconds))
            {
                return false;
            }

            started[key] = now;
            return true;
        }
    }
}
