# Run via Docker

The supported way to run the GroupDocs.Search MCP server. The image is multi-arch (`linux/amd64`,
`linux/arm64`) and carries the .NET runtime and the engine's native dependencies.

## Prerequisites

[Docker Desktop](https://www.docker.com/products/docker-desktop/) on Windows or macOS, or Docker Engine
on Linux.

## 1. Pull

```bash
docker pull ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

`:latest` follows the newest release. Pin a version tag in shared configs and CI: Docker reuses a cached
`:latest` until you pull again, so two machines can silently run different versions.

## 2. Run

MCP servers speak over stdin/stdout, so the container needs `-i` and is started by your MCP client, not
left running on its own:

```bash
docker run --rm -i \
  -v /path/to/documents:/data \
  ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

- `/data` is the documents folder. Tool paths are relative to it.
- The **index** is written to `/data/.groupdocs-search-index`, so it survives `--rm` and the next run
  finds it. The server never indexes that folder.

### Linux: run as your user

The image runs as a non-root user (uid 1654). On Linux a mounted folder keeps its host owner, so a folder
you own is read-only inside the container and the index cannot be created. Add `--user`:

```bash
docker run --rm -i --user "$(id -u):$(id -g)" \
  -v /path/to/documents:/data \
  ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

In an MCP client's JSON config there is no shell to expand `$(id -u)`; use the numbers it prints,
typically `"--user", "1000:1000"`. Docker Desktop on Windows and macOS does not need this.

### Read-only documents

Mount the documents read-only and give the index its own volume:

```bash
docker run --rm -i \
  -v /path/to/documents:/data:ro \
  -v /path/to/index:/index -e GROUPDOCS_MCP_INDEX_PATH=/index \
  ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

## 3. Smoke test the handshake

```bash
printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"smoke","version":"1"}}}' \
  '{"jsonrpc":"2.0","method":"notifications/initialized"}' \
  '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' \
  | docker run --rm -i -v "$PWD:/data" ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

Expected: two JSON-RPC responses. The first names `GroupDocs.Search.Mcp` and its version; the second
lists eight tools: `create_index`, `add_to_index`, `update_index`, `remove_from_index`, `search`,
`get_text_fragments`, `get_index_info`, `get_license_status`.

If the second response does not arrive, the server read end-of-input before answering. Real clients
keep stdin open; the piped smoke test is for a quick look only.

## 4. Licensing

Without a licence the server runs in **evaluation mode**: an index holds at most 100 documents and a
search returns at most 10. Every response carries an `evaluation` note while that applies.

Licence file:

```bash
docker run --rm -i \
  -v /path/to/documents:/data \
  -v /path/to/license-folder:/license:ro \
  -e GROUPDOCS_LICENSE_PATH=/license/GroupDocs.Total.lic \
  ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

Metered keys — forward the **names** only, so the values never appear on the command line:

```bash
docker run --rm -i \
  -e GROUPDOCS_METERED_PUBLIC_KEY -e GROUPDOCS_METERED_PRIVATE_KEY \
  -v /path/to/documents:/data \
  ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

Metered mode reports usage to GroupDocs servers and needs outbound connectivity. Call
`get_license_status` to confirm which mode is active.

## 5. Docker Compose

See [examples/docker-compose.yml](../examples/docker-compose.yml). Compose is useful for keeping the
volume layout in one file; the client still launches the server over stdio.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Path 'x' not found in storage` with a file list | The path is relative to `/data`, not to your host | Pass `contracts`, not `/home/me/contracts` |
| `documentsPath must be relative` | An absolute path was passed | Use a path relative to the mounted folder |
| Index is empty after a restart | `/data` is not mounted, so the index lived inside the container | Add `-v <folder>:/data` |
| `The index folder '/data/.groupdocs-search-index' is not writable` | Linux: the container user does not own your folder | Add `--user "$(id -u):$(id -g)"` (or the numbers in a JSON config) |
| `Read-only file system` on indexing | Documents mounted `:ro` without an index volume | Add `-v <dir>:/index -e GROUPDOCS_MCP_INDEX_PATH=/index` |
| Searches stop at 10 results | Evaluation mode | Configure a licence or metered keys |
