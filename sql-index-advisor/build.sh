#!/usr/bin/env bash
# =============================================================================
# Build script for the sql-index-advisor project.
# This script restores NuGet packages and builds the solution in Release mode.
# =============================================================================

set -euo pipefail

# Change to the directory containing the solution file if needed.
# Assuming the solution file (*.sln) is located in the root of the repository.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$REPO_ROOT"

# Restore NuGet packages and build the solution
if command -v dotnet >/dev/null 2>&1; then
    dotnet restore
    dotnet build --configuration Release
else
    echo "Error: .NET SDK (dotnet) is not installed or not in PATH."
    exit 1
fi
