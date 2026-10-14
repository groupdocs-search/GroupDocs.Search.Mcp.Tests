# Run the integration tests

The suite launches a built server, connects as an MCP stdio client, and drives every tool. It never
references the server's source.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker, for the image channel

## Choose what to test

| Variable | Launches | Use for |
|---|---|---|
| `MCP_SERVER_IMAGE` | `docker run --rm -i -v <temp>:/data <image>` | A published or locally built image — CI's channel |
| `MCP_SERVER_DLL` | `dotnet <dll>` | A local build before anything is published |
| neither | `dnx GroupDocs.Search.Mcp@<version>` | Not usable today: there is no NuGet package |

## Run

```bash
git clone https://github.com/groupdocs-search/GroupDocs.Search.Mcp.Tests.git
cd GroupDocs.Search.Mcp.Tests

MCP_SERVER_IMAGE=ghcr.io/groupdocs-search/search-net-mcp:26.10.0 \
  dotnet test -c Release --filter "Category!=Metered"
```

A locally built image works the same way:

```bash
docker build -f docker/Dockerfile -t search-net-mcp:dev /path/to/GroupDocs.Search.Mcp
MCP_SERVER_IMAGE=search-net-mcp:dev dotnet test -c Release --filter "Category!=Metered"
```

Narrow the run:

```bash
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"     # handshake only, seconds
dotnet test -c Release --filter "FullyQualifiedName~SearchTests"
dotnet test -c Release --filter "FullyQualifiedName~IndexLifecycleTests"
dotnet test -c Release --filter "FullyQualifiedName~ErrorHandlingTests"
```

## Metered licensing tests

They spend real credit, so they are a separate category and never part of the default run. Set the
two keys in the environment, then:

```bash
MCP_SERVER_IMAGE=ghcr.io/groupdocs-search/search-net-mcp:26.10.0 \
  dotnet test -c Release --filter "Category=Metered"
```

The fixture forwards the keys to Docker by name (`-e GROUPDOCS_METERED_PUBLIC_KEY`), so the values
never reach a command line. Without the keys these tests fail on purpose rather than skip.

## Expected result

```text
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31   (Category!=Metered)
Passed!  - Failed: 0, Passed:  4, Skipped: 0, Total:  4   (Category=Metered)
```

Measured 2026-10-14 against a local build of 26.10.0, on both the DLL and the image channel.

## How the suite stays reliable

- Each test creates its own workspace folder and index name, so tests never share state.
- Expected hits come from synthetic files the tests write; real samples only prove formats.
- Every failure assertion checks `isError: true` **and** the specific message.

## CI

- `integration.yml` → job **docker** runs the suite against the image (`image` dispatch input, default
  `:latest`); job **dnx** (3 OS) restores and builds only.
- `metered.yml` → weekly and on demand, with the two metered secrets.
