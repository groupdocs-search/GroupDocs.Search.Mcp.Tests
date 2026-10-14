# GroupDocs.Search.Mcp.Tests

Integration tests for the [`GroupDocs.Search.Mcp`](https://github.com/groupdocs-search/GroupDocs.Search.Mcp)
MCP server, which exposes [GroupDocs.Search](https://products.groupdocs.com/search) — local full-text
indexing and search — as AI-callable tools.

This repository validates the **published** distribution end to end: it launches the server, connects
as an MCP client over stdio, and drives every tool against real and synthetic documents. Unit tests
live in the main repo; nothing here references the server's source.

> **Docker-first distribution.** `GroupDocs.Search.Mcp` ships **only as a container image**
> (`ghcr.io/groupdocs-search/search-net-mcp`): the packed .NET tool is ~281 MiB, over NuGet.org's 250 MB
> limit. CI therefore runs the suite against the image; the `dnx` job only compiles the project.

## Documentation

- [how-to/](how-to/) — run via Docker, verify on the MCP Registry, use with Claude Desktop and VS Code, run these tests.
- [examples/](examples/) — ready-to-paste `claude-desktop.json`, `vscode-mcp.json`, `docker-compose.yml`.
- [AGENTS.md](AGENTS.md) — orientation for AI coding agents working in this repo.
- [llms.txt](llms.txt) — machine-readable summary for LLM tooling.
- [Files/README.md](Files/README.md) — provenance of the real fixtures.
- [changelog/](changelog/) — one entry per change set.

## What gets tested

| Area | Covered by |
|---|---|
| Server starts on the chosen channel; handshake | [McpServerFixture](src/GroupDocs.Search.Mcp.Tests/Fixtures/McpServerFixture.cs) |
| Server info, exactly 8 tools, descriptions and schemas, phrase guidance in `search` | [ToolDiscoveryTests](src/GroupDocs.Search.Mcp.Tests/ToolDiscoveryTests.cs) |
| `search` — single word, `OR`, `AND`, quoted phrase, unquoted-phrase hint, no-match, DOCX/PDF/TXT samples, evaluation note | [SearchTests](src/GroupDocs.Search.Mcp.Tests/SearchTests.cs) |
| `add_to_index`, `get_index_info`, `update_index` (edit + delete), `remove_from_index` (file + folder), `create_index` (open + recreate), `get_text_fragments`, index folder never indexed | [IndexLifecycleTests](src/GroupDocs.Search.Mcp.Tests/IndexLifecycleTests.cs) |
| Every failure sets `isError: true` with the promised text: unknown index, missing path, path escape, absolute path, path as index name, file-inside-folder removal, empty and missing query | [ErrorHandlingTests](src/GroupDocs.Search.Mcp.Tests/ErrorHandlingTests.cs) |
| Metered licensing engages, lifts the evaluation note, consumption never decreases, keys never echoed | [MeteredLicensingTests](src/GroupDocs.Search.Mcp.Tests/MeteredLicensingTests.cs) (category `Metered`) |

Each test creates its own workspace folder and index name, so tests sharing one server process never
see each other's state. Assertions depend only on words the tests wrote themselves.

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and, for the image
channel, Docker. Pick the server with an environment variable:

```bash
# The published (or a locally built) image - the channel CI uses
MCP_SERVER_IMAGE=ghcr.io/groupdocs-search/search-net-mcp:26.10.0 dotnet test --filter "Category!=Metered"

# A local build of the server, before anything is published
MCP_SERVER_DLL=/path/to/GroupDocs.Search.Mcp/src/GroupDocs.Search.Mcp/bin/Release/net10.0/publish/GroupDocs.Search.Mcp.dll \
  dotnet test --filter "Category!=Metered"
```

The metered tests spend real credit and need `GROUPDOCS_METERED_PUBLIC_KEY` and
`GROUPDOCS_METERED_PRIVATE_KEY` in the environment; run them deliberately with
`--filter "Category=Metered"`. The fixture forwards the keys to the container by name, so their values
never appear on a command line.

## CI

- [integration.yml](.github/workflows/integration.yml) — push, PR, nightly, `workflow_dispatch` (with an
  `image` input) and `repository_dispatch`. The **docker** job pulls the image and runs the suite;
  the 3-OS **dnx** job restores and builds only, because there is no NuGet package to launch.
- [metered.yml](.github/workflows/metered.yml) — weekly and on demand only; needs the two metered
  secrets. Never on push or PR.

## Evaluation vs licensed mode

Unlicensed, an index holds at most 100 documents and a search returns at most 10. The suite stays well
under both, and asserts that unlicensed responses carry the server's `evaluation` note. With metered
keys or a licence, `MeteredLicensingTests` asserts the note is gone.

## License

MIT — see [LICENSE](LICENSE)
