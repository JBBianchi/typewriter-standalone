#!/usr/bin/env bash
# Verify the release dry-run locally.
# Usage: ./eng/verify-release-dryrun.sh
#
# Steps:
#   1. dotnet pack -c Release
#   2. dotnet tool install --local (from pack output)
#   3. typewriter-cli generate smoke test
#   4. Cleanup
#
# Exit codes:
#   0 - all steps passed
#   1 - a step failed

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

PACK_DIR="src/Typewriter.Cli/bin/Release"
FIXTURE_PROJECT="tests/fixtures/simple/SimpleProject/SimpleProject.csproj"
FIXTURE_DIR="tests/fixtures/simple/SimpleProject"

cleanup() {
    echo "--- Cleanup ---"
    dotnet tool uninstall typewriter.cli --local 2>/dev/null || true
    rm -f dotnet-tools.json
    rm -f "$FIXTURE_DIR/UserRole.ts" "$FIXTURE_DIR/UserModel.ts"
    echo "Cleanup complete."
}

trap cleanup EXIT

# Step 1: Pack
echo "=== Step 1: dotnet pack -c Release ==="
dotnet pack -c Release
NUPKG=$(ls "$PACK_DIR"/Typewriter.Cli.*.nupkg 2>/dev/null | head -1)
if [ -z "$NUPKG" ]; then
    echo "FAIL: No .nupkg found in $PACK_DIR"
    exit 1
fi
VERSION=$(basename "$NUPKG" | sed 's/^Typewriter\.Cli\.//; s/\.nupkg$//')
echo "PASS: Pack produced $NUPKG (version $VERSION)"

# Step 2: Local tool install
echo ""
echo "=== Step 2: dotnet tool install --local ==="
dotnet tool install --local --add-source "$PACK_DIR" Typewriter.Cli \
    --create-manifest-if-needed --version "$VERSION"
echo "PASS: Tool installed successfully"

# Step 3: Restore fixture and smoke test
echo ""
echo "=== Step 3: Smoke test — typewriter-cli generate ==="
dotnet restore "$FIXTURE_PROJECT"
dotnet typewriter-cli generate \
    "$FIXTURE_DIR/Enums.tst" \
    "$FIXTURE_DIR/Interfaces.tst" \
    --project "$FIXTURE_PROJECT"
echo "PASS: typewriter-cli generate exited with code 0"

# Verify generated output files exist
if [ ! -f "$FIXTURE_DIR/UserRole.ts" ] || [ ! -f "$FIXTURE_DIR/UserModel.ts" ]; then
    echo "FAIL: Expected generated .ts files not found"
    exit 1
fi
echo "PASS: Generated files present: UserRole.ts, UserModel.ts"

echo ""
echo "=== All steps passed ==="
