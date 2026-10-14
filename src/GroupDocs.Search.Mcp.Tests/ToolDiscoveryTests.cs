using GroupDocs.Search.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Search.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class ToolDiscoveryTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ToolDiscoveryTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void ServerInfo_AdvertisesGroupDocsSearchMcp()
    {
        var info = _fixture.Client.ServerInfo;

        Assert.NotNull(info);
        Assert.Equal("GroupDocs.Search.Mcp", info!.Name);
        Assert.False(string.IsNullOrWhiteSpace(info.Version));

        _output.WriteLine($"Server: {info.Name} {info.Version}  (channel: {_fixture.Channel})");
    }

    [Fact]
    public async Task ListTools_ExposesExactlyTheSearchTools()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        foreach (var tool in catalog.All)
            _output.WriteLine($"tool: {tool.Name}");

        Assert.Equal(
            ToolCatalog.ExpectedNames.OrderBy(n => n),
            catalog.All.Select(t => t.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task AllTools_HaveDescriptionsAndObjectSchemas()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        foreach (var tool in catalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description), $"Tool '{tool.Name}' has no description.");
            Assert.Equal(System.Text.Json.JsonValueKind.Object, tool.JsonSchema.ValueKind);
            Assert.True(tool.JsonSchema.TryGetProperty("properties", out _), $"Tool '{tool.Name}' schema has no properties.");
        }
    }

    [Fact]
    public async Task SearchDescription_TellsAgentsToQuotePhrases()
    {
        // The engine matches nothing for unquoted multi-word queries. The description is the
        // only place an agent learns that before it gets an empty result.
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        Assert.Contains("double quotes", catalog.Search.Description, StringComparison.OrdinalIgnoreCase);
    }
}
