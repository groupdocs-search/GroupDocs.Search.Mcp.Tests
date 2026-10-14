using ModelContextProtocol.Client;

namespace GroupDocs.Search.Mcp.IntegrationTests.Fixtures;

/// Resolves tools by their exact wire name. Search's names are set explicitly on each
/// [McpServerTool(Name = ...)], and "search" is a substring of no other tool but would be a
/// fragile keyword, so this catalog matches whole names rather than substrings.
internal sealed class ToolCatalog
{
    public static readonly string[] ExpectedNames =
    {
        "create_index", "add_to_index", "update_index", "remove_from_index",
        "search", "get_text_fragments", "get_index_info", "get_license_status",
    };

    private readonly IReadOnlyList<McpClientTool> _tools;

    private ToolCatalog(IReadOnlyList<McpClientTool> tools) => _tools = tools;

    public static async Task<ToolCatalog> LoadAsync(McpClient client, CancellationToken ct = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return new ToolCatalog(tools.ToList());
    }

    public IReadOnlyList<McpClientTool> All => _tools;

    public McpClientTool CreateIndex      => Resolve("create_index");
    public McpClientTool AddToIndex       => Resolve("add_to_index");
    public McpClientTool UpdateIndex      => Resolve("update_index");
    public McpClientTool RemoveFromIndex  => Resolve("remove_from_index");
    public McpClientTool Search           => Resolve("search");
    public McpClientTool GetTextFragments => Resolve("get_text_fragments");
    public McpClientTool GetIndexInfo     => Resolve("get_index_info");
    public McpClientTool GetLicenseStatus => Resolve("get_license_status");

    private McpClientTool Resolve(string name) =>
        _tools.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No tool named '{name}'. Found: {string.Join(", ", _tools.Select(t => t.Name))}");
}
