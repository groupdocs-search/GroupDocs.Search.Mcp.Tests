# How-to guides

Step-by-step guides for running the GroupDocs.Search MCP server
(`ghcr.io/groupdocs-search/search-net-mcp`) and connecting AI clients to it.

| # | Guide | Use when |
|---|---|---|
| 01 | [NuGet: not available — run from source](01-install-from-nuget.md) | You want to know why there is no `dnx` command, or to run a local build. |
| 02 | [Run via Docker](02-run-via-docker.md) | The supported way to run the server. Start here. |
| 03 | [Verify on the MCP Registry](03-verify-mcp-registry.md) | You want to confirm a release is listed and installable. |
| 04 | [Use with Claude Desktop](04-use-with-claude-desktop.md) | You use Claude Desktop. |
| 05 | [Use with VS Code / GitHub Copilot](05-use-with-vscode-copilot.md) | You use VS Code agent mode. |
| 06 | [Run the integration tests](06-run-integration-tests.md) | You are verifying an image or a local build. |

## Facts every guide relies on

- **Docker-first.** The packed .NET tool is ~281 MiB, over NuGet.org's 250 MB limit, so the server
  ships as a multi-arch image only. Guides use `:26.10.1`; substitute a newer tag or `:latest`.
- **Tools on the wire:** `create_index`, `add_to_index`, `update_index`, `remove_from_index`,
  `search`, `get_text_fragments`, `get_index_info`, `get_license_status`.
- **Stateful.** An index persists between calls. By default it lives in
  `/data/.groupdocs-search-index`, inside the one documents mount.
- **Paths are relative** to the documents folder (`contracts`, `reports/q3.pdf`); indexes are named
  (`contracts`), never addressed by filesystem path.
- **Evaluation mode** (no licence): 100 documents per index, 10 documents per search result. Every
  response says so while it applies.
