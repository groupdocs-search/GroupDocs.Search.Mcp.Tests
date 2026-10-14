#!/bin/bash
################################################################################
# GroupDocs.Search.Mcp — Integration Test Runner
#
# Runs the xUnit integration suite against a built server. GroupDocs.Search.Mcp is
# Docker-first (no NuGet package: the packed tool is ~281 MiB > 250 MB), so the
# default target is the GHCR image; --dll tests a local build instead.
#
# Scenarios: ToolDiscovery, Search, IndexLifecycle, ErrorHandling
#            (+ MeteredLicensing with --metered — spends real credit)
#
# Usage:
#   ./02_test-all-scenarios.sh [OPTIONS]
#
# Options:
#   --image IMAGE      Image to test (default: ghcr.io/groupdocs-search/search-net-mcp:latest)
#   --dll PATH         Test a local build (GroupDocs.Search.Mcp.dll) instead of an image
#   --filter PATTERN   Only tests whose name contains PATTERN (e.g. SearchTests)
#   --metered          Run ONLY the Metered category (needs both metered keys in the env)
#   --license PATH     GroupDocs licence file to forward
#   --no-build         Skip the build
#   --help             Show this help
#
# Examples:
#   ./02_test-all-scenarios.sh
#   ./02_test-all-scenarios.sh --image ghcr.io/groupdocs-search/search-net-mcp:26.10.0
#   ./02_test-all-scenarios.sh --image search-net-mcp:dev --filter IndexLifecycle
#   ./02_test-all-scenarios.sh --dll <path>/GroupDocs.Search.Mcp.dll
################################################################################
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TEST_PROJECT="$PROJECT_ROOT/src/GroupDocs.Search.Mcp.Tests/GroupDocs.Search.Mcp.Tests.csproj"
IMAGE="ghcr.io/groupdocs-search/search-net-mcp:latest"
DLL=""
FILTER=""
METERED=false
LICENSE=""
SKIP_BUILD=false

usage() { sed -n '2,29p' "$0" | sed 's/^# \{0,1\}//'; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --image)    IMAGE="$2"; shift 2 ;;
    --dll)      DLL="$2"; shift 2 ;;
    --filter)   FILTER="$2"; shift 2 ;;
    --metered)  METERED=true; shift ;;
    --license)  LICENSE="$2"; shift 2 ;;
    --no-build) SKIP_BUILD=true; shift ;;
    --help)     usage; exit 0 ;;
    *)          echo "Unknown option: $1"; usage; exit 1 ;;
  esac
done

command -v dotnet >/dev/null || { echo "The .NET 10 SDK is required."; exit 1; }

if [ -n "$LICENSE" ]; then
  [ -f "$LICENSE" ] || { echo "Licence file not found: $LICENSE"; exit 1; }
  export GROUPDOCS_LICENSE_PATH="$LICENSE"
fi

if [ -n "$DLL" ]; then
  [ -f "$DLL" ] || { echo "DLL not found: $DLL"; exit 1; }
  export MCP_SERVER_DLL="$DLL"
  unset MCP_SERVER_IMAGE || true
  echo "Target: local build $DLL"
else
  command -v docker >/dev/null || { echo "Docker is required for the image channel."; exit 1; }
  export MCP_SERVER_IMAGE="$IMAGE"
  echo "Target: image $IMAGE"
  docker image inspect "$IMAGE" >/dev/null 2>&1 || docker pull "$IMAGE"
fi

if $METERED; then
  # Presence only - never print a key.
  if [ -z "${GROUPDOCS_METERED_PUBLIC_KEY:-}" ] || [ -z "${GROUPDOCS_METERED_PRIVATE_KEY:-}" ]; then
    echo "--metered needs GROUPDOCS_METERED_PUBLIC_KEY and GROUPDOCS_METERED_PRIVATE_KEY set."
    exit 1
  fi
  EXPR="Category=Metered"
else
  EXPR="Category!=Metered"
fi
[ -n "$FILTER" ] && EXPR="$EXPR&FullyQualifiedName~$FILTER"

$SKIP_BUILD || dotnet build "$TEST_PROJECT" -c Release

dotnet test "$TEST_PROJECT" -c Release --no-build --filter "$EXPR" --logger "console;verbosity=normal"
