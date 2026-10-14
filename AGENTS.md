# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

**Integration tests** for the GroupDocs.Search MCP server — the container image
`ghcr.io/groupdocs-search/search-net-mcp`, which exposes GroupDocs.Search for .NET (local full-text
indexing and search) as AI-callable tools.

This repo is **not** the server. The server lives at
[groupdocs-search/GroupDocs.Search.Mcp](https://github.com/groupdocs-search/GroupDocs.Search.Mcp). This repo:

1. Consumes only a **built artifact** — the published image, or a locally built DLL — never a project reference.
2. Launches the server, connects as an MCP stdio client, and drives every tool.
3. Doubles as copy-pasteable example configs and how-to guides.

**Docker-first.** The server is not on NuGet (packed tool ~281 MiB > 250 MB), so CI's `docker` job is
the one that runs tests; the 3-OS `dnx` job only compiles.

## Folder layout

```
src/GroupDocs.Search.Mcp.Tests/
  Fixtures/
    McpServerFixture.cs     ← launch channel: MCP_SERVER_IMAGE (docker) | MCP_SERVER_DLL | dnx
    SampleDocuments.cs      ← real samples (Files/ → <storage>/samples) + per-test synthetic workspaces
    SearchCalls.cs          ← typed helpers: Ok/Call/Index/Search/Found/FilePaths
    ToolCatalog.cs          ← exact-name tool resolution + the expected tool list
    ToolResponse.cs         ← CallToolResult text/JSON extraction
    MeteredKeys.cs          ← metered key presence (never prints values)
    CommandResolver.cs      ← resolves docker/dotnet/dnx on PATH (dnx.cmd on Windows)
    PackageVersion.cs       ← version under test from env / assembly metadata
  ToolDiscoveryTests.cs
  SearchTests.cs
  IndexLifecycleTests.cs
  ErrorHandlingTests.cs
  MeteredLicensingTests.cs  ← [Trait("Category","Metered")] - spends real credit
Files/                      ← real fixtures from GroupDocs.Search-for-.NET Examples (see Files/README.md)
.github/workflows/          ← integration.yml (docker + dnx-compile), metered.yml (weekly/dispatch)
how-to/ examples/ docker-scripts/ changelog/
Directory.Build.props       ← McpPackageVersion (26.10.1), TreatWarningsAsErrors
```

## The rules that make a stateful suite trustworthy

1. **Every test owns its state.** Use `SampleDocuments.NewWorkspace(fixture, "label")`: a fresh
   sub-folder with the two synthetic files and a unique index name. Never index or edit another test's
   folder; never rely on test order.
2. **Assert on words you wrote.** Expected hits and misses come from the synthetic texts in
   `SampleDocuments` (`revenue`, `renewal`, `"revenue forecast"`…). Real samples only prove formats.
3. **Assert `isError` and the promised text together.** Never `IsError || contains(...)` — that
   oracle passes on the defect it is meant to catch.
4. **Engine semantics are part of the contract** (verified on 26.10.0): unquoted multi-word queries
   match nothing; `remove_from_index` works on indexed paths only. Tests pin both behaviours.
5. **Metered tests hard-fail without keys** — gating lives in `metered.yml`, where it is visible.

## Commands you can run

```bash
dotnet build -c Release

# Against an image (CI's channel)
MCP_SERVER_IMAGE=ghcr.io/groupdocs-search/search-net-mcp:26.10.1 dotnet test -c Release --filter "Category!=Metered"

# Against a local server build
MCP_SERVER_DLL=<path>/GroupDocs.Search.Mcp.dll dotnet test -c Release --filter "Category!=Metered"

# Metered (spends credit; keys from the environment)
MCP_SERVER_IMAGE=... dotnet test -c Release --filter "Category=Metered"
```

## House rules

1. Changelog entry + `McpPackageVersion` bump for every change (CalVer, same number as the server).
2. No `ProjectReference` to the server; no committed licence files or keys.
3. Target framework is `net10.0` only.
