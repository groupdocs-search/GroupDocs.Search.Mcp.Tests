---
id: 001
date: 2026-10-14
version: 26.10.0
type: feature
---

# Integration suite for GroupDocs.Search.Mcp 26.10.0

## What changed

New integration-test repository for the GroupDocs.Search MCP server, cloned in shape from
GroupDocs.Parser.Mcp.Tests (the family's other Docker-first product) and rewritten for a stateful
search server.

- **Channels.** `McpServerFixture` launches the server from `MCP_SERVER_IMAGE` (docker — the CI
  channel), `MCP_SERVER_DLL` (a local build), or `dnx` (unusable until a NuGet package exists).
  Metered keys are forwarded to Docker by name only.
- **Isolation.** Every test creates its own workspace sub-folder with two synthetic text files and a
  unique index name (`SampleDocuments.NewWorkspace`), so tests sharing one server never see each
  other's state. Real samples (`Files/`, from GroupDocs.Search-for-.NET `Examples/Resources/Documents`)
  are copied to `<storage>/samples/` and only prove DOCX/PDF/TXT indexing.
- **Tests (31 + 4 metered):**
  - `ToolDiscoveryTests` — server info, exactly 8 tools, descriptions and schemas, phrase-quoting guidance.
  - `SearchTests` — word, `OR`, `AND`, quoted phrase, unquoted-phrase hint, empty result, multi-format samples, evaluation note.
  - `IndexLifecycleTests` — add, info (incl. no-argument call), update after edit and delete, remove file and folder, create and recreate, fragments, index folder never indexed.
  - `ErrorHandlingTests` — every failure asserts `isError: true` and the promised message.
  - `MeteredLicensingTests` (`Category=Metered`) — metered mode, no evaluation note, consumption never decreases, keys never echoed.
- **CI.** `integration.yml`: the docker job runs the suite against the image; the 3-OS dnx job compiles
  only. `metered.yml`: weekly and on demand.
- **Docs.** README, AGENTS.md, llms.txt, `how-to/` (Docker-first set), `examples/` (Docker configs),
  `docker-scripts/` (image-channel runner, hand smoke test). The SDK-container runner inherited from
  Parser was dropped: it only ever exercised `dnx`.

## Why

The server is the family's first stateful product: correctness depends on what was indexed before, so
tests must own their state and assert on content they wrote. The 2026-08-16 audit also showed that
permissive oracles (`IsError || contains(...)`) pass on the defects they exist to catch; every failure
test here checks the flag and the text together.

## Verification

Against a local build of GroupDocs.Search.Mcp 26.10.0 on 2026-10-14:

| Channel | `Category!=Metered` | `Category=Metered` |
|---|---|---|
| `MCP_SERVER_DLL` | 31/31 pass | — |
| `MCP_SERVER_IMAGE=search-net-mcp:dev` | 31/31 pass | 4/4 pass |

`docker-scripts/04_run-server-with-samples.sh` against the same image: 8 tools, 5 samples indexed,
"lorem" found in the three Lorem ipsum files.

## Migration impact

None — first release.
