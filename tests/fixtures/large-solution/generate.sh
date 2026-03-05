#!/usr/bin/env bash
# Generates the large-solution fixture: 25 minimal .csproj projects, a .sln file,
# and 5 .tst template files spread across selected projects.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_COUNT=25
SLN_NAME="LargeSolution"
CS_GUID="FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"

# Clean previous output (keep scripts)
find "$SCRIPT_DIR" -mindepth 1 -maxdepth 1 \
  -not -name 'generate.sh' \
  -not -name 'generate.ps1' \
  -exec rm -rf {} +

# ── helpers ──────────────────────────────────────────────────────────────────

# Deterministic GUID: A1B2C3D4-0000-0000-0000-{12-hex-zero-padded-index}
project_guid() {
  printf '{A1B2C3D4-0000-0000-0000-%012X}' "$1"
}

pad2() { printf '%02d' "$1"; }

# ── generate projects ────────────────────────────────────────────────────────

for i in $(seq 1 "$PROJECT_COUNT"); do
  NAME="Project$(pad2 "$i")"
  DIR="$SCRIPT_DIR/$NAME"
  mkdir -p "$DIR"

  # .csproj
  cat > "$DIR/$NAME.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
CSPROJ

  # Class1.cs
  cat > "$DIR/Class1.cs" <<CS
namespace $NAME;

public class Class1 { }
CS
done

# ── generate .tst files in selected projects ─────────────────────────────────

# Project03 – Enums.tst
cat > "$SCRIPT_DIR/Project03/Enums.tst" <<'TST'
$Enums(*)[
export enum $Name {
    $Values[
    $Name = $Value,]
}
]
TST

# Project07 – Interfaces.tst
cat > "$SCRIPT_DIR/Project07/Interfaces.tst" <<'TST'
$Classes(*Model)[
export interface I$Name {
    $Properties[
    $name: $Type;]
}
]
TST

# Project12 – Models.tst
cat > "$SCRIPT_DIR/Project12/Models.tst" <<'TST'
$Classes(*)[
export class $Name {
    $Properties[
    $name: $Type;]
}
]
TST

# Project18 – Services.tst
cat > "$SCRIPT_DIR/Project18/Services.tst" <<'TST'
$Interfaces(*Service)[
export interface $Name {
    $Methods[
    $name($Parameters[$name: $Type][, ]): $Type;]
}
]
TST

# Project22 – AllTypes.tst
cat > "$SCRIPT_DIR/Project22/AllTypes.tst" <<'TST'
$Classes(*)[// class $FullName
]$Interfaces(*)[// interface $FullName
]$Enums(*)[// enum $FullName
]
TST

# ── generate .sln ────────────────────────────────────────────────────────────

{
  cat <<HEADER
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
HEADER

  for i in $(seq 1 "$PROJECT_COUNT"); do
    NAME="Project$(pad2 "$i")"
    GUID="$(project_guid "$i")"
    echo "Project(\"{$CS_GUID}\") = \"$NAME\", \"$NAME\\$NAME.csproj\", \"$GUID\""
    echo "EndProject"
  done

  echo "Global"
  echo "	GlobalSection(SolutionConfigurationPlatforms) = preSolution"
  echo "		Debug|Any CPU = Debug|Any CPU"
  echo "		Release|Any CPU = Release|Any CPU"
  echo "	EndGlobalSection"
  echo "	GlobalSection(ProjectConfigurationPlatforms) = postSolution"

  for i in $(seq 1 "$PROJECT_COUNT"); do
    GUID="$(project_guid "$i")"
    for CFG in Debug Release; do
      echo "		$GUID.$CFG|Any CPU.ActiveCfg = $CFG|Any CPU"
      echo "		$GUID.$CFG|Any CPU.Build.0 = $CFG|Any CPU"
    done
  done

  echo "	EndGlobalSection"
  echo "	GlobalSection(SolutionProperties) = preSolution"
  echo "		HideSolutionNode = FALSE"
  echo "	EndGlobalSection"
  echo "EndGlobal"
} > "$SCRIPT_DIR/$SLN_NAME.sln"

echo "Generated $PROJECT_COUNT projects and $SLN_NAME.sln in $SCRIPT_DIR"
