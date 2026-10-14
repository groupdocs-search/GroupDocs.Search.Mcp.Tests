# Verify on the MCP Registry

GroupDocs.Search.Mcp is listed in the official MCP Registry as an **OCI** package — the registry entry
points at the GHCR image, not at a NuGet package.

## 1. Query the registry

```bash
curl -s "https://registry.modelcontextprotocol.io/v0/servers?search=io.github.groupdocs-search/groupdocs-search-mcp" \
  | python -m json.tool
```

Check, for the newest entry:

- `name` is `io.github.groupdocs-search/groupdocs-search-mcp`;
- `version` is the release you expect (for example `26.10.0`);
- `packages[0].registryType` is `oci` and `packages[0].identifier` is
  `ghcr.io/groupdocs-search/search-net-mcp:<same version>`;
- `description` reads *"MCP server for GroupDocs.Search — index documents and run full-text search via AI agents."*

An OCI package carries its version **only** in the identifier tag; a separate `version` field on the
package would be rejected by the registry.

## 2. Check the image is publicly pullable

The registry validates OCI entries by fetching the image manifest anonymously. A private GHCR package
fails publication with 401/403.

```bash
docker logout ghcr.io
docker pull ghcr.io/groupdocs-search/search-net-mcp:26.10.0
```

The pull must succeed without credentials. The image also carries the ownership label the registry
checks:

```bash
docker image inspect ghcr.io/groupdocs-search/search-net-mcp:26.10.0 \
  --format '{{index .Config.Labels "io.modelcontextprotocol.server.name"}}'
# io.github.groupdocs-search/groupdocs-search-mcp
```

## 3. Find it from a client

In VS Code: Command Palette → **MCP: Add Server** → browse the registry → search for
`groupdocs-search` or `search`. The registry-generated configuration runs the image with Docker.

## 4. Automated check

[06 — Run the integration tests](06-run-integration-tests.md) against the same tag proves the listed
artifact works, not just that it is listed. `ToolDiscoveryTests.ServerInfo_AdvertisesGroupDocsSearchMcp`
and `ListTools_ExposesExactlyTheSearchTools` are the quickest two.
