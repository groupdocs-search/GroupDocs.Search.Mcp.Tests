#!/bin/bash
################################################################################
# 04 — Smoke-test the GroupDocs.Search MCP image by hand
#
# Copies the repo's Files/ samples into a temp folder, mounts it at /data, and runs a
# sequenced MCP session (smoke_session.py): initialize -> tools/list -> add_to_index ->
# search "lorem". The index lands in <temp>/.groupdocs-search-index, which shows the
# default index location working with a single mount.
#
# Usage:
#   ./04_run-server-with-samples.sh [--image IMAGE] [--keep]
#
#   --image IMAGE   default ghcr.io/groupdocs-search/search-net-mcp:latest
#   --keep          keep the temp folder to inspect the index
################################################################################
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
IMAGE="ghcr.io/groupdocs-search/search-net-mcp:latest"
KEEP=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --image) IMAGE="$2"; shift 2 ;;
    --keep)  KEEP=true; shift ;;
    *)       echo "Unknown option: $1"; exit 1 ;;
  esac
done

command -v docker >/dev/null || { echo "Docker is required."; exit 1; }
PY="$(command -v python3 || command -v python || true)"
[ -n "$PY" ] || { echo "Python 3 is required for the JSON-RPC driver."; exit 1; }

WORK="$(mktemp -d)"
cp "$PROJECT_ROOT"/Files/*.docx "$PROJECT_ROOT"/Files/*.pdf "$PROJECT_ROOT"/Files/*.txt "$WORK"/
if ! $KEEP; then trap 'rm -rf "$WORK"' EXIT; fi

HOST_PATH="$WORK"
DRIVER="$SCRIPT_DIR/smoke_session.py"
# Git Bash: hand Windows paths to Docker and Python, and stop MSYS rewriting the "-v x:/data" pair.
if command -v cygpath >/dev/null; then
  HOST_PATH="$(cygpath -w "$WORK")"
  DRIVER="$(cygpath -w "$DRIVER")"
fi

MSYS_NO_PATHCONV=1 "$PY" "$DRIVER" "$IMAGE" "$HOST_PATH"
echo "OK"
if $KEEP; then echo "Kept: $WORK"; fi
