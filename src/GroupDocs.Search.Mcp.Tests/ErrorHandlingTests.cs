using GroupDocs.Search.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Search.Mcp.IntegrationTests;

/// Every failure must carry isError: true AND a readable message. The oracles assert both,
/// and the specific text promised — never "IsError || contains(anything)", which passes on the
/// defect it is meant to catch (2026-08-16 audit).
[Collection(McpServerCollection.Name)]
public class ErrorHandlingTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ErrorHandlingTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private async Task<string> Failure(string tool, Dictionary<string, object?> args)
    {
        var result = await _fixture.Call(tool, args);
        var text = ToolResponse.Text(result);
        _output.WriteLine(text);
        Assert.True(result.IsError ?? false, $"[{_fixture.Channel}] expected isError: true from {tool}. Body:\n{text}");
        return text;
    }

    [Fact]
    public async Task Search_UnknownIndex_NamesItAndListsExistingIndexes()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "known");
        await _fixture.Index(ws);

        var text = await Failure("search", new() { ["query"] = "x", ["indexName"] = "ghost" });

        Assert.StartsWith("Search failed for 'ghost'", text);
        Assert.Contains($"'{ws.IndexName}'", text);
    }

    [Fact]
    public async Task AddToIndex_MissingPath_ListsWhatExists()
    {
        var text = await Failure("add_to_index", new() { ["documentsPath"] = "no-such-folder" });

        Assert.Contains("not found in storage", text);
        Assert.Contains("Available files and folders:", text);
        Assert.Contains($"- {SampleDocuments.SamplesFolder}/", text);
    }

    [Theory]
    [InlineData("../")]
    [InlineData("../../etc")]
    public async Task AddToIndex_PathEscape_IsRejected(string path)
    {
        var text = await Failure("add_to_index", new() { ["documentsPath"] = path });

        Assert.Contains("outside the storage folder", text);
    }

    [Fact]
    public async Task AddToIndex_AbsolutePath_IsRejected()
    {
        var text = await Failure("add_to_index", new() { ["documentsPath"] = "/etc" });

        Assert.Contains("relative to the storage folder", text);
    }

    [Fact]
    public async Task IndexName_ThatIsAPath_IsRejected()
    {
        var text = await Failure("get_index_info", new() { ["indexName"] = "../outside" });

        Assert.Contains("indexName must be a plain name", text);
    }

    [Fact]
    public async Task RemoveFromIndex_FileInsideIndexedFolder_ExplainsTheEngineRule()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "rmrule");
        await _fixture.Index(ws);

        var text = await Failure("remove_from_index", new()
        {
            ["documentPath"] = ws.File(SampleDocuments.AlphaReport),
            ["indexName"] = ws.IndexName,
        });

        Assert.StartsWith($"Removal failed for '{ws.File(SampleDocuments.AlphaReport)}'", text);
        Assert.Contains("whole indexed paths only", text);
        Assert.Contains($"'{ws.Relative}'", text);
    }

    [Fact]
    public async Task Search_EmptyQuery_IsRejected()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "empty");
        await _fixture.Index(ws);

        var text = await Failure("search", new() { ["query"] = " ", ["indexName"] = ws.IndexName });

        Assert.Contains("query must not be empty", text);
    }

    [Fact]
    public async Task Search_MissingRequiredQuery_IsReported()
    {
        var text = await Failure("search", new() { ["indexName"] = "default" });

        Assert.Contains("query", text);
    }
}
