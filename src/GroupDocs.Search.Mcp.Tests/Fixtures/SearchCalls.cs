using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace GroupDocs.Search.Mcp.IntegrationTests.Fixtures;

/// Typed wrappers over the raw tool calls, so tests read as the scenario they check.
internal static class SearchCalls
{
    public static async Task<CallToolResult> Call(
        this McpServerFixture fixture, string tool, Dictionary<string, object?> args, bool slow = false)
    {
        return slow
            ? await fixture.Client.CallToolAsync(tool, args, cancellationToken: McpServerFixture.Patience())
            : await fixture.Client.CallToolAsync(tool, args);
    }

    /// Calls a tool that must succeed; a failure reports the channel and the server's own text.
    public static async Task<CallToolResult> Ok(
        this McpServerFixture fixture, string tool, Dictionary<string, object?> args, bool slow = false)
    {
        var result = await fixture.Call(tool, args, slow);
        Assert.False(result.IsError ?? false,
            $"[{fixture.Channel}] {tool} failed: {ToolResponse.Text(result)}");
        return result;
    }

    public static Task<CallToolResult> Index(this McpServerFixture fixture, Workspace ws, string? documentsPath = null) =>
        fixture.Ok("add_to_index", new()
        {
            ["documentsPath"] = documentsPath ?? ws.Relative,
            ["indexName"] = ws.IndexName,
        }, slow: true);

    public static async Task<JsonElement> Search(this McpServerFixture fixture, string indexName, string query) =>
        ToolResponse.Json(await fixture.Ok("search", new() { ["query"] = query, ["indexName"] = indexName }));

    public static int Found(this JsonElement search) => search.GetProperty("documentsFound").GetInt32();

    public static string[] FilePaths(this JsonElement search) =>
        search.GetProperty("documents").EnumerateArray()
            .Select(d => d.GetProperty("filePath").GetString() ?? string.Empty)
            .ToArray();
}
