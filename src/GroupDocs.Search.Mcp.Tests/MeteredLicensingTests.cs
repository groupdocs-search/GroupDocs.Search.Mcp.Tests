using System.Text.Json;
using GroupDocs.Search.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Search.Mcp.IntegrationTests;

/// <summary>
/// Proves metered licensing actually engages on the real artifact.
/// </summary>
/// <remarks>
/// <para><b>These tests spend real money.</b> Every billable operation against a metered key
/// consumes credit, so they are excluded from the default run by their Metered category and
/// triggered deliberately - weekly or on demand. Use a dedicated CI metered account with a
/// capped balance, never the production key.</para>
///
/// <para>They do not skip when unconfigured: a missing key is a hard failure, because the
/// gating lives in the CI job where it is visible.</para>
///
/// <para>Search ships Docker-only, so these run against the image: set MCP_SERVER_IMAGE
/// (see <see cref="McpServerFixture"/>).</para>
/// </remarks>
[Trait("Category", "Metered")]
[Collection(McpServerCollection.Name)]
public class MeteredLicensingTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public MeteredLicensingTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        MeteredKeys.RequireConfigured();
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task GetLicenseStatus_ReportsMeteredMode()
    {
        var json = ToolResponse.Json(await _fixture.Ok("get_license_status", new()));
        _output.WriteLine(json.ToString());

        Assert.Equal("metered", json.GetProperty("mode").GetString());
        Assert.True(json.GetProperty("licensed").GetBoolean());
        Assert.Equal("GroupDocs.Search", json.GetProperty("engine").GetProperty("name").GetString());

        Assert.True(json.TryGetProperty("consumption", out var consumption), "Metered mode must report consumption.");
        Assert.False(consumption.TryGetProperty("error", out _), $"Consumption reading failed: {consumption}");
        Assert.True(consumption.TryGetProperty("quantity", out var quantity) && quantity.ValueKind == JsonValueKind.Number);
    }

    [Fact]
    public async Task Search_CarriesNoEvaluationNote()
    {
        // The evaluation limits are counts (100 indexed / 10 returned), invisible in the data
        // itself; the server's evaluation note is the observable signal. Under metered it must
        // be gone, or licensing did not engage.
        var ws = SampleDocuments.NewWorkspace(_fixture, "metered");
        await _fixture.Index(ws);

        var result = await _fixture.Search(ws.IndexName, "revenue");

        Assert.Equal(1, result.Found());
        Assert.False(result.TryGetProperty("evaluation", out _),
            $"[{_fixture.Channel}] search still reports evaluation mode under metered keys.");
    }

    [Fact]
    public async Task Consumption_NeverGoesBackwards()
    {
        var before = await ReadQuantityAsync();

        var ws = SampleDocuments.NewWorkspace(_fixture, "consumption");
        await _fixture.Index(ws);
        await _fixture.Search(ws.IndexName, "renewal");

        var after = await ReadQuantityAsync();
        _output.WriteLine($"consumption before={before} after={after} delta={after - before}");

        Assert.True(after >= before, $"Consumption went backwards ({before} -> {after}).");
    }

    [Fact]
    public async Task LicenseStatus_NeverEchoesTheKeys()
    {
        var text = ToolResponse.Text(await _fixture.Ok("get_license_status", new()));

        Assert.DoesNotContain(MeteredKeys.PrivateKey!, text, StringComparison.Ordinal);
        Assert.DoesNotContain(MeteredKeys.PublicKey!, text, StringComparison.Ordinal);
    }

    private async Task<decimal> ReadQuantityAsync()
    {
        var consumption = ToolResponse.Json(await _fixture.Ok("get_license_status", new())).GetProperty("consumption");
        return consumption.TryGetProperty("quantity", out var q) && q.ValueKind == JsonValueKind.Number ? q.GetDecimal() : 0m;
    }
}
