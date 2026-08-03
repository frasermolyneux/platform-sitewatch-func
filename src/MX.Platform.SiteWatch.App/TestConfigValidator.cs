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

        foreach (var test in tests)
        {
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
