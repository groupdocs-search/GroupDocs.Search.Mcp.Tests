#!/bin/bash
# GroupDocs.Search.Mcp.Tests — cheatsheet. Copy the lines you need; not meant to be run as-is.

# 0. Preflight: Docker, .NET 10 SDK, project layout
./01_verify-setup.sh

# 1. Whole suite against the latest published image (what CI's docker job does)
./02_test-all-scenarios.sh

# 2. Pin a release
./02_test-all-scenarios.sh --image ghcr.io/groupdocs-search/search-net-mcp:26.10.0

# 3. A locally built image (in the main repo: docker build -f docker/Dockerfile -t search-net-mcp:dev .)
./02_test-all-scenarios.sh --image search-net-mcp:dev

# 4. A local build of the server, no Docker
./02_test-all-scenarios.sh --dll /path/to/GroupDocs.Search.Mcp/src/GroupDocs.Search.Mcp/bin/Release/net10.0/publish/GroupDocs.Search.Mcp.dll

# 5. One area only
./02_test-all-scenarios.sh --filter SearchTests
./02_test-all-scenarios.sh --filter IndexLifecycleTests
./02_test-all-scenarios.sh --filter ErrorHandlingTests

# 6. Metered licensing (spends real credit; the keys must already be in the environment)
./02_test-all-scenarios.sh --metered

# 7. Hand smoke test of an image: handshake, tool list, index the samples, search "lorem"
./04_run-server-with-samples.sh --image ghcr.io/groupdocs-search/search-net-mcp:26.10.0
