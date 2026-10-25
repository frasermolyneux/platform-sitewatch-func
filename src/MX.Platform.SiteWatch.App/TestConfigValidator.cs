namespace MX.Platform.SiteWatch.App;

/// <summary>
/// Validates the explicit telemetry contract on <see cref="TestConfig"/> entries at startup. Fails fast
/// (consistent with the other required-app-setting checks in <c>Program.cs</c>) rather than allowing the
/// timer to emit availability results with a missing or inconsistent tenant/component identifier.
/// </summary>
internal static class TestConfigValidator
{
    public static void Validate(IReadOnlyList<TestConfig> tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        if (tests.GroupBy(test => (test.Site, test.EffectiveCheckId)).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Availability check identities must be unique within their owning site.");
        }

        foreach (var test in tests)
        {
            if (test.IntervalSeconds is < 30 or > 3600 || test.IntervalSeconds % 30 != 0
                || test.Retries is < 0 or > 3 || test.Method is not ("GET" or "HEAD")
                || (test.CheckId is not null && string.IsNullOrWhiteSpace(test.CheckId)))
            {
                throw new InvalidOperationException($"Availability test '{test.App}' has an invalid cadence, method, retry budget or check identity.");
            }

            if (string.IsNullOrWhiteSpace(test.Site))
            {
                throw new InvalidOperationException($"Availability test '{test.App}' is missing required 'site' (tenant/site id).");
            }

            if (!string.IsNullOrWhiteSpace(test.Component) && !test.Component.StartsWith($"{test.Site}.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Availability test '{test.App}' has component '{test.Component}' that does not start with its site prefix '{test.Site}.'.");
            }
        }
    }
}
