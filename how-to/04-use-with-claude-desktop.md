# Use with Claude Desktop

Let Claude index a folder of your documents and answer questions from them — which files mention
something, what exactly they say — **locally**, without uploading anything.

## Prerequisites

- [Claude Desktop](https://claude.ai/download)
- Docker ([02 — Run via Docker](02-run-via-docker.md))
- A folder of documents (PDF, Word, Excel, PowerPoint, email, text, archives…)

## 1. Add the server

Open **Settings → Developer → Edit Config** and add:

```json
{
  "mcpServers": {
    "groupdocs-search": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "-v", "/absolute/path/to/documents:/data", "ghcr.io/groupdocs-search/search-net-mcp:26.10.1"]
    }
  }
}
```

On Windows the host path looks like `C:\\Users\\me\\Documents\\contracts:/data` (escaped backslashes).
The same file is in [examples/claude-desktop.json](../examples/claude-desktop.json).

Restart Claude Desktop.

## 2. Verify the connection

Open the tools menu in a new chat. Under `groupdocs-search` you should see: `create_index`,
`add_to_index`, `update_index`, `remove_from_index`, `search`, `get_text_fragments`, `get_index_info`,
`get_license_status`.

## 3. Example prompts

```text
Index everything in my documents folder.
Which contracts mention a termination penalty?
Show me the paragraphs that talk about "force majeure", with the file each one comes from.
I edited some files in reports — refresh the index and search for the Q3 forecast again.
Create an index called hr for the HR folder, then search it for parental leave.
```

Claude calls `add_to_index` first when nothing is indexed yet, then `search` to find files and
`get_text_fragments` to read the matching passages.

Tips that save a round trip:

- Multi-word phrases need double quotes in the query. Claude learns this from the tool description;
  when it forgets, the empty result carries a hint and it retries.
- Indexes persist. After the first run, Claude can search immediately; ask it to "refresh the index"
  after files change.

## 4. Licensing

Without a licence: 100 documents per index, 10 documents per search. To license, add before the image
name either a licence file:

```json
"-v", "/absolute/path/to/license-folder:/license:ro", "-e", "GROUPDOCS_LICENSE_PATH=/license/GroupDocs.Total.lic",
```

or metered keys forwarded from your OS environment (set them there, not in this file):

```json
"-e", "GROUPDOCS_METERED_PUBLIC_KEY", "-e", "GROUPDOCS_METERED_PRIVATE_KEY",
```

On macOS, Claude Desktop launched from Finder does not see shell exports — use `launchctl setenv` or
start it from a terminal.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Server not listed | Check the JSON is valid and Docker is running; restart Claude Desktop |
| `docker: command not found` in the MCP log | Use the absolute path to the Docker binary in `command` |
| "not found in storage" | Ask for paths relative to the documents folder you mounted |
| Results stop at 10 documents | Evaluation mode — add a licence or metered keys |
