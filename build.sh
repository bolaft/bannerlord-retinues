#!/usr/bin/env bash
set -euo pipefail

# Defaults (overridden by Local.props and flags)
BL="13"
DEPLOY="true"
RUN_MAIN="true"
RUN_PREFABS="true"
RUN_STRINGS="true"
RELEASE_PATCH="" # when set, force "release" and bump last version segment
MODULE="Retinues" # default

# Print a framed header with lines of '=' above and below and a blank line before/after.
# Usage: print_header "=   Some Header   ="
print_header() {
    local hdr="$1"
    local len=${#hdr}
    printf "\n"
    printf '%*s\n' "$len" "" | tr ' ' '='
    printf '%s\n' "$hdr"
    printf '%*s\n' "$len" "" | tr ' ' '='
    printf "\n"
}

usage() {
  cat <<'USAGE'
Usage:
  ./build.sh [options]

Options:
      --no-deploy       Do not copy to game Modules
      --prefabs         Only run prefabs generation
      --no-prefabs      Skip prefabs generation
      --strings         Only run strings.py
      --no-strings      Skip strings.py
  -v, --version         Bannerlord version: 12 or 13 (default: 13)
  -r, --release <N>     Build Release and set <Version value="vX.Y.Z.N" /> to N
  -h, --help            Show help
USAGE
  exit 1
}

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    -v|--version) BL="$2"; shift 2;;
    --no-deploy) DEPLOY="false"; shift;;
    --prefabs) RUN_MAIN="false"; RUN_STRINGS="false"; shift;;
    --no-prefabs) RUN_PREFABS="false"; shift;;
    --strings) RUN_MAIN="false"; RUN_PREFABS="false"; shift;;
    --no-strings) RUN_STRINGS="false"; shift;;
    -r|--release)
      RELEASE_PATCH="${2:-}"
      [[ -z "$RELEASE_PATCH" ]] && { echo "ERROR: --release requires a numeric argument (the last version segment)"; exit 2; }
      [[ "$RELEASE_PATCH" =~ ^[0-9]+$ ]] || { echo "ERROR: --release value must be numeric, got: $RELEASE_PATCH"; exit 2; }
      shift 2
      ;;
    -h|--help) usage;;
    *)
      echo "Unknown arg: $1"
      usage
      ;;
  esac
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
MAIN_PROJ="$ROOT_DIR/src/${MODULE}/${MODULE}.csproj"
STRINGS_PY="$ROOT_DIR/loc/strings.py"

# Compute msbuild -p: args
MSBUILD_PROPS=()

if [[ "${DEPLOY}" == "false" ]]; then
  MSBUILD_PROPS+=("-p:DeployToGame=false")
else
  MSBUILD_PROPS+=("-p:DeployToGame=true")
fi
if [[ -n "$BL" ]]; then
  MSBUILD_PROPS+=("-p:BL=$BL")
fi

# Config: Release when --release is used, otherwise use default (dev)
if [[ -n "$RELEASE_PATCH" ]]; then
  MSBUILD_CONFIG=(-c Release)
else
  MSBUILD_CONFIG=(-c Debug)
fi

# If release, bump version in SubModule*.xml files (for the selected module)
bump_submodule_version() {
  local patch="$1"
  local core_dir="$ROOT_DIR/src/${MODULE}"
  local changed=0

  local files=(
    "$core_dir/SubModule.BL12.xml"
    "$core_dir/SubModule.BL13.xml"
    "$core_dir/SubModule.xml"
  )

  for f in "${files[@]}"; do
    if [[ -f "$f" ]]; then
      local tmp="${f}.tmp.$$"
      sed -E 's/(<Version[[:space:]]+value="v[0-9]+\.[0-9]+\.[0-9]+\.)([0-9]+)"/\1'"$patch"'"/' "$f" > "$tmp"
      mv "$tmp" "$f"
      echo "  - Set version patch -> $patch in $(basename "$f")"
      changed=1
    fi
  done

  if [[ $changed -eq 0 ]]; then
    echo "  (No SubModule*.xml found under ${core_dir}; skipped)"
  fi
}

# Banner
print_header "=   ${MODULE} Build   ="
echo "  BL      : $BL"
echo "  Build   : $RUN_MAIN"
echo "  Prefabs : $RUN_PREFABS"
echo "  Strings : $RUN_STRINGS"
echo "  Config  : ${MSBUILD_CONFIG[1]}"
echo "  Deploy  : $DEPLOY"
if [[ -n "$RELEASE_PATCH" ]]; then
  echo "  Release : $RELEASE_PATCH"
fi

# If --release N is set, bump SubModule version before building
if [[ -n "$RELEASE_PATCH" ]]; then
  print_header "=   Updating SubModule version (${MODULE})   ="
  bump_submodule_version "$RELEASE_PATCH"
fi

# 1) Prefabs
if [[ "$RUN_PREFABS" == "true" ]]; then
  print_header "=   Rendering Prefabs   ="
  python tpl/render_prefabs.py --version "$BL"
fi

# 1.b) Deploy prefabs-only (if requested)
if [[ "$RUN_PREFABS" == "true" && "$DEPLOY" == "true" && -f "$MAIN_PROJ" ]]; then
  print_header "=   Deploying ${MODULE} GUI   ="
  echo "Copying generated GUI to module..."
  dotnet msbuild "$MAIN_PROJ" -t:DeployPrefabsOnly -p:BL="$BL" -p:DeployToGame=true -p:ModuleName="${MODULE}"
fi

# 2) Strings
if [[ "$RUN_STRINGS" == "true" && -f "$STRINGS_PY" ]]; then
  print_header "=   Compiling Strings   ="
  python "$STRINGS_PY" --module "$MODULE"
fi

# 3) Main project
if [[ "$RUN_MAIN" == "true" && -f "$MAIN_PROJ" ]]; then
  print_header "=   Building ${MODULE}   ="
  dotnet build "$MAIN_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}" -p:ModuleName="${MODULE}"
fi

# Done
print_header "=   Build Finished   ="
echo "✅ $(date)"
