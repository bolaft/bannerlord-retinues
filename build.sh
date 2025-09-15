#!/usr/bin/env bash
set -euo pipefail

# ---------------------------
# Defaults (override with -g)
# ---------------------------
GAME_DIR_DEFAULT='C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord'
CONFIG_DEFAULT='Debug'

# ---------------------------
# Parse args
# ---------------------------
GAME_DIR="$GAME_DIR_DEFAULT"
CONFIG="$CONFIG_DEFAULT"
RUN_PREFABS=1
PREFABS_ONLY=0
BUILD_MCM=0

usage() {
  echo "Usage: $0 [-g \"<Bannerlord Game Dir>\"] [-c Debug|Release] [--no-prefabs] [--prefabs-only] [--MCM]"
  echo "  -g, --game-dir      Path to the Mount & Blade II Bannerlord folder"
  echo "  -c, --config        Build configuration (Debug/Release). Default: $CONFIG_DEFAULT"
  echo "      --no-prefabs    Skip running PrefabBuilder"
  echo "      --prefabs-only  Only run PrefabBuilder"
  echo "      --MCM           Build Retinues.MCM"
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -g|--game-dir)
      [[ $# -lt 2 ]] && usage
      GAME_DIR="$2"
      shift 2
      ;;
    -c|--config)
      [[ $# -lt 2 ]] && usage
      CONFIG="$2"
      shift 2
      ;;
    --no-prefabs)
      RUN_PREFABS=0
      shift
      ;;
    --prefabs-only)
      PREFABS_ONLY=1
      shift
      ;;
    --MCM)
      BUILD_MCM=1
      shift
      ;;
    -h|--help)
      usage
      ;;
    *)
      echo "Unknown arg: $1"
      usage
      ;;
  esac
done

# ---------------------------
# Resolve paths
# ---------------------------
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PREFAB_PROJ="$ROOT_DIR/src/PrefabBuilder/PrefabBuilder.csproj"
CORE_PROJ="$ROOT_DIR/src/Retinues/Core/Retinues.Core.csproj"
MCM_PROJ="$ROOT_DIR/src/Retinues/MCM/Retinues.MCM.csproj"

echo "== Retinues build =="
echo " Game dir : $GAME_DIR"
echo " Config   : $CONFIG"
echo " Root     : $ROOT_DIR"
echo

# ---------------------------
# Sanity checks
# ---------------------------
if [[ ! -f "$CORE_PROJ" ]]; then
  echo "ERROR: Core project not found at: $CORE_PROJ" >&2
  exit 2
fi
if [[ ! -f "$MCM_PROJ" ]]; then
  echo "ERROR: MCM project not found at: $MCM_PROJ" >&2
  exit 2
fi
if [[ $RUN_PREFABS -eq 1 && ! -f "$PREFAB_PROJ" ]]; then
  echo "WARN: PrefabBuilder project not found, skipping prefab generation." >&2
  RUN_PREFABS=0
fi

# Optional: pre-check the Bannerlord bin path
BL_BIN="$GAME_DIR/bin/Win64_Shipping_Client"
if [[ ! -d "$BL_BIN" ]]; then
  echo "WARN: Bannerlord bin not found at: $BL_BIN" >&2
  echo "     If this path is wrong, pass -g \"<correct path>\"." >&2
fi

# ---------------------------
# 1) Build & run PrefabBuilder (optional)
# ---------------------------
if [[ $RUN_PREFABS -eq 1 ]]; then
  echo "== Building PrefabBuilder =="
  dotnet build "$PREFAB_PROJ" -c "$CONFIG"
  echo "== Running PrefabBuilder =="
  dotnet run --no-build --project "$PREFAB_PROJ"
  echo
fi

if [[ $PREFABS_ONLY -eq 1 ]]; then
  echo "== Prefabs-only mode: skipping Core and MCM builds =="
  echo "== Build finished ✅ =="
  exit 0
fi

# ---------------------------
# 2) Build Core or MCM
# ---------------------------
if [[ $BUILD_MCM -eq 1 ]]; then
  echo "== Building Retinues.MCM =="
  dotnet build "$MCM_PROJ" -c "$CONFIG" \
    -p:BannerlordGameDir="$GAME_DIR"

else
  # Run strings.py before building Core
  STRINGS_SCRIPT="$ROOT_DIR/strings.py"
  if [[ -f "$STRINGS_SCRIPT" ]]; then
    echo "== Running strings.py =="
    python "$STRINGS_SCRIPT"
  else
    echo "WARN: strings.py not found at $STRINGS_SCRIPT, skipping."
  fi

  echo "== Building Retinues.Core =="
  dotnet build "$CORE_PROJ" -c "$CONFIG" \
    -p:BannerlordGameDir="$GAME_DIR"
fi


echo "== Build finished ✅ == ($(date))"
