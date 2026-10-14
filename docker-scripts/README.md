# Docker scripts

Bash helpers for running the GroupDocs.Search MCP integration suite and smoke-testing the image. They
work in Git Bash or WSL on Windows, and on Linux and macOS.

GroupDocs.Search.Mcp is **Docker-first**: there is no NuGet package (the packed tool is ~281 MiB, over
the 250 MB limit), so these scripts target the GHCR image by default, or a local build with `--dll`.

## Files

| File | Purpose |
|---|---|
| `00_quick-start.sh` | Cheatsheet of the common invocations. Not meant to be run as-is. |
| `01_verify-setup.sh` | Preflight: Docker, .NET 10 SDK, project layout, `Files/` fixtures, connectivity. |
| `02_test-all-scenarios.sh` | Runs the suite against an image (`--image`, default `:latest`) or a local build (`--dll`). `--metered` runs only the paid metered tests. |
| `04_run-server-with-samples.sh` | Hand smoke test: mounts the `Files/` samples, indexes them, searches "lorem", checks 8 tools. |
| `smoke_session.py` | The JSON-RPC driver `04` uses. |
| `helpers.sh` | Shared logging and Docker/.NET utilities from the family template. |

## Quick start

```bash
cd docker-scripts
./01_verify-setup.sh
./02_test-all-scenarios.sh --image ghcr.io/groupdocs-search/search-net-mcp:26.10.1
./04_run-server-with-samples.sh --image ghcr.io/groupdocs-search/search-net-mcp:26.10.1
```

Expected from `04`:

```text
server: {'name': 'GroupDocs.Search.Mcp', 'version': '26.10.1'}
tools (8): add_to_index, create_index, get_index_info, get_license_status, get_text_fragments, remove_from_index, search, update_index
add_to_index: Indexed the whole storage folder into index 'default': 5 document(s) added, 5 in the index.
search 'lorem': 3 document(s): Lorem ipsum.docx, Lorem ipsum.pdf, Lorem ipsum.txt
OK
```

The order of the three Lorem files depends on relevance.

## Licensing

Evaluation mode (no licence) is enough for every non-metered test. To exercise metered licensing, put
`GROUPDOCS_METERED_PUBLIC_KEY` and `GROUPDOCS_METERED_PRIVATE_KEY` in the environment and run
`./02_test-all-scenarios.sh --metered`. The fixture forwards the keys to Docker by name, so the values
never appear on a command line. These tests spend real credit.
