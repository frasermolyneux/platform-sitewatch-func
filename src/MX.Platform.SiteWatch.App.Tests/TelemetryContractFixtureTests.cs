using System.Text.Json;

namespace MX.Platform.SiteWatch.App.Tests;

/// <summary>
/// Guards the cross-repository telemetry contract fixture (<c>contract/availability-telemetry-contract.json</c>,
/// duplicated verbatim in platform-status-web). This test asserts the dimension names this producer
/// emits (see <see cref="ComponentDimensionTests"/>) exactly match the fixture's declared
/// <c>customDimensions</c> keys, so a rename here that isn't mirrored in the fixture (and in
/// platform-status-web's copy/tests) fails CI instead of silently drifting in production.
/// </summary>
public sealed class TelemetryContractFixtureTests
{
    private static readonly string[] EmittedDimensionNames = ["componentId", "siteId", "region"];

    [Fact]
    public void EmittedDimensionNames_MatchContractFixture()
    {
        using var document = JsonDocument.Parse(ReadContractFixture());
        var declaredDimensions = document.RootElement
            .GetProperty("customDimensions")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(EmittedDimensionNames.OrderBy(name => name, StringComparer.Ordinal), declaredDimensions.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void ContractFixture_MarksAllDimensionsRequired()
    {
        using var document = JsonDocument.Parse(ReadContractFixture());
        foreach (var property in document.RootElement.GetProperty("customDimensions").EnumerateObject())
        {
            Assert.True(property.Value.GetProperty("required").GetBoolean(), $"Dimension '{property.Name}' should be marked required in the contract fixture.");
        }
    }

    private static string ReadContractFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "contract", "availability-telemetry-contract.json");
        return File.ReadAllText(path);
    }
}
