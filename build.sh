#!/usr/bin/env bash
set -euo pipefail

# Defaults (overridden by Local.props and flags)
BL="13"
DEPLOY="true"
RUN_MAIN="true"
RUN_PREFABS="true"
RUN_STRINGS="true"
RELEASE_PATCH="" # when set, force "release" and bump last version segment

usage() {
  cat <<'USAGE'
Usage:
  ./build.sh [options]

Options:
      --no-deploy       Do not copy to game Modules
      --prefabs         Only run PrefabBuilder
      --no-prefabs      Skip PrefabBuilder
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
MAIN_PROJ="$ROOT_DIR/src/Retinues/Retinues.csproj"
PREFABS_PROJ="$ROOT_DIR/src/PrefabBuilder/PrefabBuilder.csproj"
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

# Config: Release when --release is used, otherwise use your default (dev)
if [[ -n "$RELEASE_PATCH" ]]; then
  MSBUILD_CONFIG=(-c Release)
else
  MSBUILD_CONFIG=(-c Debug)
fi

# If release, bump version in SubModule*.xml files
bump_submodule_version() {
  local patch="$1"
  local core_dir="$ROOT_DIR/src/Retinues"
  local changed=0

  # Which files to try (edit if you only use one naming scheme)
  local files=(
    "$core_dir/SubModule.BL12.xml"
    "$core_dir/SubModule.BL13.xml"
    "$core_dir/SubModule.xml"
  )

  for f in "${files[@]}"; do
    if [[ -f "$f" ]]; then
      # Replace last numeric segment of vX.Y.Z.N with provided patch number
      # Works even if X.Y.Z differ between BL12 and BL13.
      # Make a backup, then move back over it (portable across sed variants)
      local tmp="${f}.tmp.$$"
      # shellcheck disable=SC2001
      sed -E 's/(<Version[[:space:]]+value="v[0-9]+\.[0-9]+\.[0-9]+\.)([0-9]+)"/\1'"$patch"'"/' "$f" > "$tmp"
      mv "$tmp" "$f"
      echo "  - Set version patch -> $patch in $(basename "$f")"
      changed=1
    fi
  done
}

# Banner
echo "== Retinues build =="
echo " BL      : $BL"
echo " Build   : $RUN_MAIN"
echo " Prefabs : $RUN_PREFABS"
echo " Strings : $RUN_STRINGS"
echo " Config  : ${MSBUILD_CONFIG[1]}"
echo " Deploy  : $DEPLOY"
if [[ -n "$RELEASE_PATCH" ]]; then
  echo " Release : $RELEASE_PATCH"
fi
echo

# If --release N is set, bump SubModule version before building
if [[ -n "$RELEASE_PATCH" ]]; then
  echo "== Updating SubModule version =="
  bump_submodule_version "$RELEASE_PATCH"
  echo
fi

# 1) Prefabs
if [[ "$RUN_PREFABS" == "true" && -f "$PREFABS_PROJ" ]]; then
  echo "== Building PrefabBuilder =="
  dotnet build "$PREFABS_PROJ" "${MSBUILD_CONFIG[@]}"
  echo

  echo "== Running PrefabBuilder =="
  PREFAB_OUT="$ROOT_DIR/gui"
  TPL_TEMPLATES="$ROOT_DIR/tpl/templates"
  TPL_PARTIALS="$ROOT_DIR/tpl/partials"

  rm -rf "$PREFAB_OUT"
  mkdir -p "$PREFAB_OUT"

  dotnet run --no-build --configuration "${MSBUILD_CONFIG[1]}" --project "$PREFABS_PROJ" -- \
    --out "$PREFAB_OUT" \
    --templates "$TPL_TEMPLATES" \
    --partials "$TPL_PARTIALS" \
    --bl "$BL" \
    --config "${MSBUILD_CONFIG[1]}" \
    --module "Retinues"

  echo
fi

# 1.b) Deploy prefabs-only (if requested)
if [[ "$RUN_PREFABS" == "true" && "$DEPLOY" == "true" && -f "$MAIN_PROJ" ]]; then
  echo "== Deploying generated GUI to module =="
  # BL and DeployToGame flow through as MSBuild props
  dotnet msbuild "$MAIN_PROJ" -t:DeployPrefabsOnly -p:BL="$BL" -p:DeployToGame=true
  echo
fi

# 2) Strings
if [[ "$RUN_STRINGS" == "true" && -f "$STRINGS_PY" ]]; then
  echo "== Running strings.py =="
    python "$STRINGS_PY"
  echo
fi

# 3) Main project
if [[ "$RUN_MAIN" == "true" && -f "$MAIN_PROJ" ]]; then
  echo "== Building Retinues =="
    dotnet build "$MAIN_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
  echo
fi

# Done
echo "== Build finished ✅ == ($(date))"
