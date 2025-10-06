#!/usr/bin/env bash
set -euo pipefail

# Defaults (overridden by Local.props and flags)
BL="13"
GAME_DIR=""
TARGET="core" # core|mcm|prefabs|all
DEPLOY="true"
RUN_PREFABS="auto" # auto|yes|no
RELEASE_PATCH=""   # when set, force "release" and bump last version segment

usage() {
  cat <<'USAGE'
Usage:
  ./build.sh [options]

Options:
  -t, --target          core | mcm | prefabs | all         (default: core)
  -g, --game-dir        Path to Bannerlord folder          (overrides Local.props)
      --no-deploy       Do not copy to game Modules
      --deploy          Copy to game Modules (default)
      --prefabs         Force run PrefabBuilder before build
      --no-prefabs      Skip PrefabBuilder
  -v, --version         Bannerlord version: 12 or 13 (default: 13)
  -r, --release <N>     Build Release and set <Version value="vX.Y.Z.N" /> to N
  -h, --help            Show help

Examples:
  ./build.sh -t core --release 7
  ./build.sh -t all --no-deploy --prefabs --bl 12
USAGE
  exit 1
}

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    -t|--target) TARGET="$2"; shift 2;;
    -g|--game-dir) GAME_DIR="$2"; shift 2;;
    -v|--version) BL="$2"; shift 2;;
    --no-deploy) DEPLOY="false"; shift;;
    --deploy) DEPLOY="true"; shift;;
    --prefabs) RUN_PREFABS="yes"; shift;;
    --no-prefabs) RUN_PREFABS="no"; shift;;
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
PREFAB_PROJ="$ROOT_DIR/src/PrefabBuilder/PrefabBuilder.csproj"
CORE_PROJ="$ROOT_DIR/src/Retinues/Core/Retinues.Core.csproj"
MCM_PROJ="$ROOT_DIR/src/Retinues/MCM/Retinues.MCM.csproj"

# Compute msbuild -p: args
MSBUILD_PROPS=()

if [[ -n "${GAME_DIR}" ]]; then
  MSBUILD_PROPS+=("-p:BannerlordGameDir=${GAME_DIR}")
fi
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
  MSBUILD_CONFIG=(-c dev)  # your repo's default; overridden by $(DefaultConfiguration) if you set it
fi

# Prefabs auto: run if project exists and target includes core/all
if [[ "${RUN_PREFABS}" == "auto" ]]; then
  if [[ -f "$PREFAB_PROJ" && ( "$TARGET" == "core" || "$TARGET" == "all" || "$TARGET" == "prefabs" ) ]]; then
    RUN_PREFABS="yes"
  else
    RUN_PREFABS="no"
  fi
fi

# If release, bump version in SubModule*.xml files
bump_submodule_version() {
  local patch="$1"
  local core_dir="$ROOT_DIR/src/Retinues/Core"
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

  if [[ "$changed" -eq 0 ]]; then
    echo "WARN: No SubModule*.xml found under $core_dir to update version."
  fi
}

# Banner
echo "== Retinues build =="
echo " BL      : $BL"
echo " Target  : $TARGET"
echo " Config  : ${MSBUILD_CONFIG[1]}"
[[ -n "${GAME_DIR}" ]] && echo " Game    : $GAME_DIR"
echo " Deploy  : $DEPLOY"
echo " Prefabs : $RUN_PREFABS"
if [[ -n "$RELEASE_PATCH" ]]; then
  echo " Release : patch=$RELEASE_PATCH (will update SubModule*.xml and build Release)"
fi
echo

# If --release N is set, bump SubModule version before building
if [[ -n "$RELEASE_PATCH" ]]; then
  echo "== Updating SubModule version =="
  bump_submodule_version "$RELEASE_PATCH"
  echo
fi

# 1) Prefabs
if [[ "$RUN_PREFABS" == "yes" && -f "$PREFAB_PROJ" ]]; then
  echo "== Building PrefabBuilder =="
  dotnet build "$PREFAB_PROJ"

  echo "== Running PrefabBuilder =="
  PREFAB_OUT="$ROOT_DIR/bin/Modules/Retinues.Core/GUI"
  TPL_TEMPLATES="$ROOT_DIR/tpl/templates"
  TPL_PARTIALS="$ROOT_DIR/tpl/partials"

  mkdir -p "$PREFAB_OUT"

  dotnet run --no-build --project "$PREFAB_PROJ" -- \
    --out "$PREFAB_OUT" \
    --templates "$TPL_TEMPLATES" \
    --partials "$TPL_PARTIALS" \
    --bl "$BL" \
    --config "${MSBUILD_CONFIG[1]}" \
    --module "Retinues.Core"

  echo
fi

# 2) Main targets
case "$TARGET" in
  core)
    dotnet build "$CORE_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    ;;
  mcm)
    dotnet build "$MCM_PROJ"  "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    ;;
  prefabs)
  # already done; nothing else
    ;;
  all)
    dotnet build "$CORE_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    dotnet build "$MCM_PROJ"  "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    ;;
  *)
    echo "Unknown target: $TARGET"; exit 2;;
esac

echo "== Build finished ✅ == ($(date))"
