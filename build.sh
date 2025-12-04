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
BUILD_PY="$ROOT_DIR/build.py"
SUBMODULE_YAML="$ROOT_DIR/build.yaml"

# Try to resolve BannerlordGameDir the same way as MSBuild:
# 1. Environment variable BANNERLORD_GAME_DIR (if set)
# 2. Retinues.Local.props <BannerlordGameDir> override (if present)
# 3. Default from Directory.Build.props
DEFAULT_BANNERLORD_GAME_DIR='C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord'
LOCAL_PROPS="$ROOT_DIR/Retinues.Local.props"

if [[ -n "${BANNERLORD_GAME_DIR:-}" ]]; then
  GAME_DIR="$BANNERLORD_GAME_DIR"
elif [[ -f "$LOCAL_PROPS" ]]; then
  # Extract first BannerlordGameDir value from Local.props if any
  GAME_DIR_LINE="$(grep -oP '<BannerlordGameDir>\K.*?(?=</BannerlordGameDir>)' "$LOCAL_PROPS" || true)"
  if [[ -n "$GAME_DIR_LINE" ]]; then
    GAME_DIR="$GAME_DIR_LINE"
  else
    GAME_DIR="$DEFAULT_BANNERLORD_GAME_DIR"
  fi
else
  GAME_DIR="$DEFAULT_BANNERLORD_GAME_DIR"
fi

# Matches Directory.Build.targets: $(BannerlordGameDir)\Modules\$(MSBuildProjectName)\
MODULE_DEPLOY_DIR="${GAME_DIR}\\Modules\\${MODULE}\\"

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

# 1) Prefabs
if [[ "$RUN_PREFABS" == "true" ]]; then
  print_header "=   Rendering Prefabs   ="
  python tpl/prefabs.py --version "$BL"
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

# 4) Generate SubModule.xml directly into the deployed module folder
if [[ "$RUN_MAIN" == "true" && "$DEPLOY" == "true" ]]; then
  if [[ -f "$BUILD_PY" && -f "$SUBMODULE_YAML" ]]; then
    print_header "=   Generating SubModule.xml   ="
    if [[ -n "$RELEASE_PATCH" ]]; then
      python "$BUILD_PY" \
        --config "$SUBMODULE_YAML" \
        --only "$BL" \
        --out "$MODULE_DEPLOY_DIR" \
        --release-patch "$RELEASE_PATCH"
    else
      python "$BUILD_PY" \
        --config "$SUBMODULE_YAML" \
        --only "$BL" \
        --out "$MODULE_DEPLOY_DIR"
    fi
  else
    echo "⚠️  Skipping SubModule.xml generation: build.py or build.yaml not found."
    echo "    Expected:"
    echo "      $BUILD_PY"
    echo "      $SUBMODULE_YAML"
  fi
fi

# 5) If this is a release build, also prepare the Steam Workshop / zip package
if [[ "$RUN_MAIN" == "true" && "$DEPLOY" == "true" && -n "$RELEASE_PATCH" ]]; then
  if [[ -f "$BUILD_PY" && -f "$SUBMODULE_YAML" ]]; then
    print_header "=   Preparing Release Package   ="
    python "$BUILD_PY" \
      --config "$SUBMODULE_YAML" \
      --only "$BL" \
      --release-patch "$RELEASE_PATCH" \
      --module-dir "$MODULE_DEPLOY_DIR" \
      --package-release
  else
    echo "⚠️  Skipping release packaging: build.py or build.yaml not found."
  fi
fi

# Done
print_header "=   Build Finished   ="
echo "✅ $(date)"
