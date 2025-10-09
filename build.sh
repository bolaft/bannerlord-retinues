#!/usr/bin/env bash
set -euo pipefail

# Defaults (overridden by Local.props and flags)
BL="13"
GAME_DIR=""
TARGET="retinues"   # retinues|prefabs|all
DEPLOY="true"
RUN_PREFABS="auto"  # auto|yes|no
RELEASE_PATCH=""    # when set, force Release and bump last version segment
DEBUG="${DEBUG:-0}"

usage() {
  cat <<'USAGE'
Usage:
  ./build.sh [options]

Options:
  -t, --target          retinues | prefabs | all       (default: retinues)
  -g, --game-dir        Path to Bannerlord folder      (overrides Local.props)
      --no-deploy       Do not copy to game Modules
      --deploy          Copy to game Modules (default)
      --prefabs         Force run PrefabBuilder
      --no-prefabs      Skip PrefabBuilder
  -v, --version         Bannerlord version: 12 or 13 (default: 13)
  -r, --release <N>     Build Release and set <Version value="vX.Y.Z.N" /> to N
  -h, --help            Show help

Examples:
  ./build.sh -t retinues --release 7
  ./build.sh --prefabs --deploy --version 13
USAGE
  exit 1
}

# ---------- small helper to deploy GUI tree if we only ran prefabs ----------
deploy_gui() {
  local src="$ROOT_DIR/bin/Modules/Retinues/GUI"
  if [[ ! -d "$src" ]]; then
    echo "No GUI dir to deploy (missing: $src)"; return 0
  fi

  if [[ -n "$GAME_DIR" ]]; then
    local dest="$GAME_DIR/Modules/Retinues/GUI"
    echo "== Deploying GUI to game dir =="
    echo "  from: $src"
    echo "  to  : $dest"
    mkdir -p "$dest"
    if command -v rsync >/dev/null 2>&1; then
      rsync -a "$src"/ "$dest"/
    else
      cp -r "$src"/. "$dest"/
    fi
  else
    echo "== Deploying GUI via MSBuild target (BannerlordGameDir from props) =="
    dotnet build "$RET_PROJ" -p:DeployToGame=true -t:DeployRetinuesGui -v:m
  fi
}

# ---------- parse args ----------
while [[ $# -gt 0 ]]; do
  case "$1" in
    -t|--target) TARGET="$2"; shift 2;;
    -g|--game-dir) GAME_DIR="$2"; shift 2;;
    -v|--version) BL="$2"; shift 2;;
    --no-deploy) DEPLOY="false"; shift;;
    --deploy) DEPLOY="true"; shift;;
    --prefabs) RUN_PREFABS="yes"; TARGET="prefabs"; shift;;
    --no-prefabs) RUN_PREFABS="no"; shift;;
    -r|--release)
      RELEASE_PATCH="${2:-}"
      [[ -z "$RELEASE_PATCH" ]] && { echo "ERROR: --release requires a numeric argument (the last version segment)"; exit 2; }
      [[ "$RELEASE_PATCH" =~ ^[0-9]+$ ]] || { echo "ERROR: --release value must be numeric, got: $RELEASE_PATCH"; exit 2; }
      shift 2
      ;;
    -h|--help) usage;;
    *) echo "Unknown arg: $1"; usage;;
  esac
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PREFAB_PROJ="$ROOT_DIR/src/PrefabBuilder/PrefabBuilder.csproj"
RET_PROJ="$ROOT_DIR/src/Retinues/Retinues.csproj"

PREFAB_OUT="${PREFAB_OUT:-$ROOT_DIR/bin/Modules/Retinues/GUI}"
TPL_TEMPLATES="${TPL_TEMPLATES:-$ROOT_DIR/tpl/templates}"
TPL_PARTIALS="${TPL_PARTIALS:-$ROOT_DIR/tpl/partials}"

# Run localization script before anything else
if [[ -f "$ROOT_DIR/loc/strings.py" ]]; then
  echo "== Generating localization files =="
  python "$ROOT_DIR/loc/strings.py" || { echo "ERROR: loc/strings.py failed"; exit 3; }
  echo
fi

# ---------- msbuild props ----------
MSBUILD_PROPS=()
[[ -n "$GAME_DIR" ]] && MSBUILD_PROPS+=("-p:BannerlordGameDir=${GAME_DIR}")
MSBUILD_PROPS+=("-p:DeployToGame=${DEPLOY}")
MSBUILD_PROPS+=("-p:BL=$BL")

# Config: Release when --release is used, otherwise use your default (Debug)
if [[ -n "$RELEASE_PATCH" ]]; then
  MSBUILD_CONFIG=(-c Release)
else
  MSBUILD_CONFIG=(-c Debug)
fi

# Auto decide prefabs
if [[ "${RUN_PREFABS}" == "auto" ]]; then
  if [[ -f "$PREFAB_PROJ" && ( "$TARGET" == "retinues" || "$TARGET" == "all" || "$TARGET" == "prefabs" ) ]]; then
    RUN_PREFABS="yes"
  else
    RUN_PREFABS="no"
  fi
fi

# ---------- bump SubModule version if --release N ----------
bump_submodule_version() {
  local patch="$1"
  local mod_dir="$ROOT_DIR/src/Retinues"
  local changed=0
  local files=(
    "$mod_dir/SubModule.BL12.xml"
    "$mod_dir/SubModule.BL13.xml"
    "$mod_dir/SubModule.xml"
  )

  # sed/mv can return non-zero on some Windows sed builds; don't kill the script.
  set +e
  for f in "${files[@]}"; do
    [[ -f "$f" ]] || continue

    # Only bump the LAST segment of vX.Y.Z.N
    local tmp="${f}.tmp.$$"
    sed -E 's/(<Version[[:space:]]+value="v[0-9]+\.[0-9]+\.[0-9]+\.)([0-9]+)"/\1'"$patch"'"/' "$f" > "$tmp"
    if cmp -s "$f" "$tmp"; then
      rm -f "$tmp"
      continue
    fi

    mv -f "$tmp" "$f"
    echo "  - Set version patch -> $patch in $(basename "$f")"
    changed=1
  done
  set -e
  set -e
  if [[ "$changed" -eq 0 ]]; then
    echo "No version bump needed (already at patch $patch) under $mod_dir."
  fi
}


echo "== Retinues build =="
echo " BL      : $BL"
echo " Target  : $TARGET"
echo " Config  : ${MSBUILD_CONFIG[1]}"
[[ -n "$GAME_DIR" ]] && echo " Game    : $GAME_DIR"
echo " Deploy  : $DEPLOY"
echo " Prefabs : $RUN_PREFABS"
[[ -n "$RELEASE_PATCH" ]] && echo " Release : patch=$RELEASE_PATCH"
echo

if [[ -n "$RELEASE_PATCH" ]]; then
   echo "== Updating SubModule version =="
   bump_submodule_version "$RELEASE_PATCH"
   echo
fi

# PrefabBuilder build
if [[ "$RUN_PREFABS" == "yes" && -f "$PREFAB_PROJ" ]]; then
  echo "== Building PrefabBuilder ==" 
  [[ "$DEBUG" == "1" ]] && echo "+ dotnet build $PREFAB_PROJ -v:m"
  dotnet build "$PREFAB_PROJ" -v:m

  echo "== Running PrefabBuilder =="
  [[ "$DEBUG" == "1" ]] && echo "+ BL=$BL CONFIG=${MSBUILD_CONFIG[1]} dotnet run --no-build --project $PREFAB_PROJ -- --out $PREFAB_OUT --templates $TPL_TEMPLATES --partials $TPL_PARTIALS --bl $BL --config ${MSBUILD_CONFIG[1]} --module Retinues"
  BL="$BL" CONFIG="${MSBUILD_CONFIG[1]}" dotnet run --no-build --project "$PREFAB_PROJ" -- \
    --out "$PREFAB_OUT" \
    --templates "$TPL_TEMPLATES" \
    --partials "$TPL_PARTIALS" \
    --bl "$BL" \
    --config "${MSBUILD_CONFIG[1]}" \
    --module "Retinues"
fi

# Deploy GUI if only prefabs were requested
if [[ "$TARGET" == "prefabs" && "$DEPLOY" == "true" ]]; then
  deploy_gui
fi

# ---------- 2) Main targets ----------
case "$TARGET" in
  retinues)
    [[ "$DEBUG" == "1" ]] && echo "+ dotnet build $RET_PROJ ${MSBUILD_CONFIG[*]} ${MSBUILD_PROPS[*]} -v:m"
    dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}" -v:m

    if [[ "$DEPLOY" == "true" ]]; then
      [[ "$DEBUG" == "1" ]] && echo "+ dotnet build $RET_PROJ ${MSBUILD_CONFIG[*]} ${MSBUILD_PROPS[*]} -t:DeployToGame -v:m"
      dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}" -t:DeployToGame -v:m
    fi
    ;;
  prefabs)
    dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    if [[ "$DEPLOY" == "true" ]]; then
      dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}" -t:DeployToGame -v:m
    fi
    ;;
  all)
    dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}"
    if [[ "$DEPLOY" == "true" ]]; then
      dotnet build "$RET_PROJ" "${MSBUILD_CONFIG[@]}" "${MSBUILD_PROPS[@]}" -t:DeployToGame -v:m
    fi
    ;;
  *)
    echo "Unknown target: $TARGET"; exit 2;;
esac

echo "== Build finished ✅ == ($(date))"
