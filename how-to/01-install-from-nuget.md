# NuGet: not available — run from source

Most GroupDocs MCP servers run with one command, `dnx GroupDocs.<Product>.Mcp --yes`. **GroupDocs.Search
does not**, and this page explains why and what to do instead.

## Why there is no NuGet package

A .NET tool package carries the whole published server. For Search that is the GroupDocs.Search engine
DLL (about 200 MiB on its own) plus ONNX Runtime and SkiaSharp native libraries. The package measures
**~281 MiB**; NuGet.org rejects anything over **250 MB**. Stripping more native runtimes cannot close
the gap, because the managed engine alone is most of it.

So the server ships as a container image instead — see [02 — Run via Docker](02-run-via-docker.md).
If a slimmer engine ships, the NuGet channel comes back; the main repo keeps the revert notes in
`.github/workflows/publish_prod.yml`.

## Run a local build instead of Docker

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/groupdocs-search/GroupDocs.Search.Mcp.git
cd GroupDocs.Search.Mcp
dotnet publish src/GroupDocs.Search.Mcp -c Release
```

Point an MCP client at the published DLL:

```json
{
  "mcpServers": {
    "groupdocs-search-local": {
      "command": "dotnet",
      "args": ["/absolute/path/to/GroupDocs.Search.Mcp/src/GroupDocs.Search.Mcp/bin/Release/net10.0/publish/GroupDocs.Search.Mcp.dll"],
      "env": { "GROUPDOCS_MCP_STORAGE_PATH": "/absolute/path/to/documents" }
    }
  }
}
```

The index is kept in `<documents>/.groupdocs-search-index` unless you set `GROUPDOCS_MCP_INDEX_PATH`.

On Linux, install the engine's native dependencies first:

```bash
sudo apt-get install -y --no-install-recommends libgdiplus libfontconfig1
```
