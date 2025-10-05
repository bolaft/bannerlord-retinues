#!/usr/bin/env bash
set -euo pipefail

# Defaults (overridden by Local.props and flags)
CONFIG=""
GAME_DIR=""
TARGET="core" # core|mcm|prefabs|all
DEPLOY="true"
RUN_PREFABS="auto" # auto|yes|no

usage() {
  cat <<'USAGE'
Usage:
  ./build.sh [options]

Options:
  -t, --target          core | mcm | prefabs | all               (default: core)
  -c, --config          dev | release                            (default: from Local.props or dev)
  -g, --game-dir        Path to Bannerlord folder                (overrides Local.props)
      --no-deploy       Do not copy to game Modules
      --deploy          Copy to game Modules (default)
      --prefabs         Force run PrefabBuilder before build
      --no-prefabs      Skip PrefabBuilder
  -h, --help            Show help

Examples:
  ./build.sh -t core -c release
  ./build.sh -t all --no-deploy --prefabs
USAGE
  exit 1
}

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    -t|--target) TARGET="$2"; shift 2;;
    -c|--config) CONFIG="$2"; shift 2;;
    -g|--game-dir) GAME_DIR="$2"; shift 2;;
    --no-deploy) DEPLOY="false"; shift;;
    --deploy) DEPLOY="true"; shift;;
    --prefabs) RUN_PREFABS="yes"; shift;;
    --no-prefabs) RUN_PREFABS="no"; shift;;
    -h|--help) usage;;
    *) echo "Unknown arg: $1"; usage;;
  esac
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
SLN_DIR="$ROOT_DIR"

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

# If no config provided, let MSBuild default to $(DefaultConfiguration)
if [[ -n "${CONFIG}" ]]; then
  MSBUILD_CONFIG=(-c "${CONFIG}")
else
  MSBUILD_CONFIG=(-c dev) # overridden by $(DefaultConfiguration) inside props
fi

# Prefabs auto: run if project exists and target includes core/all
if [[ "${RUN_PREFABS}" == "auto" ]]; then
  if [[ -f "$PREFAB_PROJ" && ( "$TARGET" == "core" || "$TARGET" == "all" || "$TARGET" == "prefabs" ) ]]; then
    RUN_PREFABS="yes"
  else
    RUN_PREFABS="no"
  fi
fi

echo "== Retinues build =="
echo " Target : $TARGET"
echo " Config : ${MSBUILD_CONFIG[1]}"
[[ -n "${GAME_DIR}" ]] && echo " Game   : $GAME_DIR"
echo " Deploy : $DEPLOY"
echo " Prefab : $RUN_PREFABS"
echo

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
    --partials "$TPL_PARTIALS"
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
