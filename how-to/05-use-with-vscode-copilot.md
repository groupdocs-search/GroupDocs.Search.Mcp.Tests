# Use with VS Code / GitHub Copilot

Search your documents from Copilot agent mode — index a folder once, then ask questions across it.

## Prerequisites

- VS Code with GitHub Copilot (agent mode)
- Docker ([02 — Run via Docker](02-run-via-docker.md))

## 1. Add the server

Use the **Install in VS Code** button in the
[main repo README](https://github.com/groupdocs-search/GroupDocs.Search.Mcp#installation), or create
`.vscode/mcp.json`:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "storage_path",
      "description": "Folder with the documents to index. The search index is kept in its .groupdocs-search-index sub-folder.",
      "password": false
    }
  ],
  "servers": {
    "groupdocs-search": {
      "type": "stdio",
      "command": "docker",
      "args": ["run", "--rm", "-i", "-v", "${input:storage_path}:/data", "ghcr.io/groupdocs-search/search-net-mcp:26.10.0"]
    }
  }
}
```

The same file is in [examples/vscode-mcp.json](../examples/vscode-mcp.json). VS Code prompts for the
folder the first time the server starts.

## 2. Verify

Open Copilot Chat in **Agent** mode → tools icon. Under `groupdocs-search` you should see the eight
tools: `create_index`, `add_to_index`, `update_index`, `remove_from_index`, `search`,
`get_text_fragments`, `get_index_info`, `get_license_status`.

## 3. Example prompts

```text
Index the contracts folder.
Which documents mention "late delivery"? Show the matching sentences.
Refresh the index — I added new files to reports.
What is in the default index, and which other indexes exist?
```

## 4. Licensing

Evaluation mode allows 100 documents per index and 10 per search. Add a licence file mount and
`GROUPDOCS_LICENSE_PATH`, or forward metered keys by name (`"-e", "GROUPDOCS_METERED_PUBLIC_KEY", "-e",
"GROUPDOCS_METERED_PRIVATE_KEY"`) with the values set in your OS environment or via `${env:...}`.
Never commit a literal key in a project `.vscode/mcp.json`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tools list is empty | Run **MCP: List Servers → groupdocs-search → Show Output**; usually Docker is not running |
| Prompted for the folder every time | Replace `${input:storage_path}` with an absolute path |
| Searches return nothing for a phrase | Quote it: `"late delivery"` — unquoted multi-word queries do not match |
