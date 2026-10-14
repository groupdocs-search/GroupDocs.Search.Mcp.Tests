using GroupDocs.Search.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Search.Mcp.IntegrationTests;

/// The stateful half of the server: create, add, update, remove, inspect. Each test owns a
/// workspace folder and an index name, so state never leaks between tests.
[Collection(McpServerCollection.Name)]
public class IndexLifecycleTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public IndexLifecycleTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task AddToIndex_ReportsDocumentsAdded()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "add");

        var text = ToolResponse.Text(await _fixture.Index(ws));
        _output.WriteLine(text);

        Assert.Contains("2 document(s) added", text);
        Assert.Contains($"'{ws.IndexName}'", text);
    }

    [Fact]
    public async Task GetIndexInfo_DescribesAnIndexBeforeAndAfterIndexing()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "info");

        var before = ToolResponse.Json(await _fixture.Ok("get_index_info", new() { ["indexName"] = ws.IndexName }));
        Assert.False(before.GetProperty("exists").GetBoolean());

        await _fixture.Index(ws);

        var after = ToolResponse.Json(await _fixture.Ok("get_index_info", new() { ["indexName"] = ws.IndexName }));
        _output.WriteLine(after.ToString());
        Assert.True(after.GetProperty("exists").GetBoolean());
        Assert.Equal(2, after.GetProperty("documentCount").GetInt32());
        Assert.Equal(ws.Relative, Assert.Single(after.GetProperty("indexedPaths").EnumerateArray()).GetString());
        Assert.False(string.IsNullOrEmpty(after.GetProperty("version").GetString()));
        Assert.Contains(after.GetProperty("availableIndexes").EnumerateArray(), i => i.GetString() == ws.IndexName);
    }

    [Fact]
    public async Task GetIndexInfo_NoArguments_IsSafe()
    {
        // The installer's verify probe calls this with no arguments.
        var info = ToolResponse.Json(await _fixture.Ok("get_index_info", new()));

        Assert.Equal("default", info.GetProperty("index").GetString());
        Assert.True(info.TryGetProperty("exists", out _));
    }

    [Fact]
    public async Task UpdateIndex_PicksUpAnEditedFile()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "update");
        await _fixture.Index(ws);
        Assert.Equal(0, (await _fixture.Search(ws.IndexName, "penalty")).Found());

        await Task.Delay(1100); // modification-time resolution
        await File.WriteAllTextAsync(Path.Combine(ws.Full, SampleDocuments.BetaNotes),
            "The renewal now includes a penalty clause for late delivery.");

        var text = ToolResponse.Text(await _fixture.Ok("update_index", new() { ["indexName"] = ws.IndexName }, slow: true));
        _output.WriteLine(text);

        Assert.Contains("1 updated", text);
        Assert.Equal(1, (await _fixture.Search(ws.IndexName, "penalty")).Found());
    }

    [Fact]
    public async Task UpdateIndex_DropsADeletedFile()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "delete");
        await _fixture.Index(ws);

        File.Delete(Path.Combine(ws.Full, SampleDocuments.AlphaReport));
        await _fixture.Ok("update_index", new() { ["indexName"] = ws.IndexName }, slow: true);

        Assert.Equal(0, (await _fixture.Search(ws.IndexName, "revenue")).Found());
    }

    [Fact]
    public async Task RemoveFromIndex_IndexedFile_StopsMatching()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "rmfile");
        await _fixture.Index(ws, ws.File(SampleDocuments.AlphaReport));
        await _fixture.Index(ws, ws.File(SampleDocuments.BetaNotes));

        await _fixture.Ok("remove_from_index", new()
        {
            ["documentPath"] = ws.File(SampleDocuments.AlphaReport),
            ["indexName"] = ws.IndexName,
        });

        Assert.Equal(0, (await _fixture.Search(ws.IndexName, "revenue")).Found());
        Assert.Equal(1, (await _fixture.Search(ws.IndexName, "renewal")).Found());
    }

    [Fact]
    public async Task RemoveFromIndex_IndexedFolder_EmptiesTheIndex()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "rmfolder");
        await _fixture.Index(ws);

        var text = ToolResponse.Text(await _fixture.Ok("remove_from_index", new()
        {
            ["documentPath"] = ws.Relative,
            ["indexName"] = ws.IndexName,
        }));

        Assert.Contains("2 document(s) removed", text);
        Assert.Equal(0, (await _fixture.Search(ws.IndexName, "revenue OR renewal")).Found());
    }

    [Fact]
    public async Task CreateIndex_OpensExisting_AndRecreateEmptiesIt()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "create");
        await _fixture.Index(ws);

        var opened = ToolResponse.Text(await _fixture.Ok("create_index", new() { ["indexName"] = ws.IndexName }));
        Assert.Contains("already exists", opened);
        Assert.Contains("2 document(s)", opened);

        var recreated = ToolResponse.Text(await _fixture.Ok("create_index", new()
        {
            ["indexName"] = ws.IndexName,
            ["recreate"] = true,
        }));
        Assert.Contains("recreated", recreated);

        var info = ToolResponse.Json(await _fixture.Ok("get_index_info", new() { ["indexName"] = ws.IndexName }));
        Assert.Equal(0, info.GetProperty("documentCount").GetInt32());
    }

    [Fact]
    public async Task GetTextFragments_ReturnsTheMatchingSentence()
    {
        var ws = SampleDocuments.NewWorkspace(_fixture, "fragments");
        await _fixture.Index(ws);

        var text = ToolResponse.Text(await _fixture.Ok("get_text_fragments", new()
        {
            ["query"] = "renewal",
            ["indexName"] = ws.IndexName,
        }));
        _output.WriteLine(text);

        Assert.Contains($"=== {ws.File(SampleDocuments.BetaNotes)}", text);
        Assert.Contains("contract renewal meeting", text);
    }

    [Fact]
    public async Task IndexFolder_IsNeverIndexedItself()
    {
        // With no GROUPDOCS_MCP_INDEX_PATH the index lives inside the storage folder. Indexing the
        // whole storage twice must not pick up the index's own files.
        var index = $"root-{Guid.NewGuid().ToString("N")[..8]}";
        await _fixture.Ok("add_to_index", new() { ["indexName"] = index }, slow: true);
        var first = ToolResponse.Json(await _fixture.Ok("get_index_info", new() { ["indexName"] = index }))
            .GetProperty("documentCount").GetInt32();

        await _fixture.Ok("add_to_index", new() { ["indexName"] = index }, slow: true);
        var info = ToolResponse.Json(await _fixture.Ok("get_index_info", new() { ["indexName"] = index }));

        var second = info.GetProperty("documentCount").GetInt32();
        _output.WriteLine($"first={first} second={second}");
        Assert.Equal(first, second);
        Assert.Empty(info.GetProperty("documentsWithErrors").EnumerateArray());
    }
}
