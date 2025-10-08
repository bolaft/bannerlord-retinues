#!/usr/bin/env bash
set -euo pipefail

BASE="$(cd -- "$(dirname -- "$0")" && pwd -P)"
SRC_BASE="$BASE/dll"
OUT_BASE="$BASE/src/TaleWorlds"

# Auto-detect versions under ./dll (dirs named with digits), or hardcode: VERSIONS=(12 13)
mapfile -t VERSIONS < <(find "$SRC_BASE" -maxdepth 1 -mindepth 1 -type d -printf '%f\n' | grep -E '^[0-9]+' | sort)
# Fallback if none found:
[[ ${#VERSIONS[@]} -eq 0 ]] && VERSIONS=(12 13)

EXCLUDE_REGEX='^(System(\..*)?|Microsoft(\..*)?|mscorlib|netstandard|WindowsBase|PresentationFramework|PresentationCore)$'
# To include everything, set: EXCLUDE_REGEX=''

if ! command -v ilspycmd >/dev/null 2>&1; then
  echo "[ERROR] ilspycmd not found. Install:"
  echo "  dotnet tool install -g ilspycmd"
  echo "Then re-open shell so PATH has \$HOME/.dotnet/tools"
  exit 1
fi

mkdir -p "$OUT_BASE"
INDEX_FILE="$OUT_BASE/index.txt"
{
  printf 'Decompile Index (generated %(%Y-%m-%d %H:%M:%S)T)\n' -1
  echo "=================================================="
  echo
} > "$INDEX_FILE"

for ver in "${VERSIONS[@]}"; do
  SRC_VER="$SRC_BASE/$ver"
  OUT_VER="$OUT_BASE/$ver"

  if [[ ! -d "$SRC_VER" ]]; then
    echo "[WARN] Skipping version $ver (folder not found: $SRC_VER)"
    { echo "[WARN] Skipping version $ver (folder not found: $SRC_VER)"; echo; } >> "$INDEX_FILE"
    continue
  fi

  mkdir -p "$OUT_VER"
  echo; echo "=== Version $ver ==="
  echo "=== Version $ver ===" >> "$INDEX_FILE"

  shopt -s nullglob
  dlls=( "$SRC_VER"/*.dll )
  if (( ${#dlls[@]} == 0 )); then
    echo "  [warn] No DLLs found in $SRC_VER"
    echo "  [warn] No DLLs found in $SRC_VER" >> "$INDEX_FILE"
    continue
  fi

  for dll in "${dlls[@]}"; do
    asm="$(basename "${dll%.*}")"
    if [[ -n "$EXCLUDE_REGEX" && "$asm" =~ $EXCLUDE_REGEX ]]; then
      echo "  [skip] $(basename "$dll")"
      echo "    [skip] $(basename "$dll")" >> "$INDEX_FILE"
      continue
    fi

    OUT_DIR="$OUT_VER/$asm"
    mkdir -p "$OUT_DIR"

    echo "  [decompile] $(basename "$dll") -> $OUT_DIR"
    if ilspycmd -p -o "$OUT_DIR" "$dll" >"$OUT_DIR/_ilspy.log" 2>&1; then
      echo "    [ok] $(basename "$dll")"
      echo "    $(basename "$dll") -> $OUT_DIR" >> "$INDEX_FILE"
    else
      echo "    [error] Failed: $(basename "$dll") (see $OUT_DIR/_ilspy.log)"
      echo "    [error] $(basename "$dll")" >> "$INDEX_FILE"
    fi
  done
  echo >> "$INDEX_FILE"
done

# --- cleanup first ---
echo "Cleaning up logs/csproj/Properties..."
for ver in "${VERSIONS[@]}"; do
  base="$OUT_BASE/$ver"
  [[ -d "$base" ]] || continue
  find "$base" -type f -name '_ilspy.log' -delete
  find "$base" -type f -name '*.csproj'   -delete
  find "$base" -type d -name 'Properties' -prune -exec rm -rf {} +
done

# Flag: set to true to enable building flat mirrors
BUILD_FLAT_MIRRORS=false

if [ "$BUILD_FLAT_MIRRORS" = true ]; then
  echo "Building flat mirrors..."

  # We intentionally relax error exit just for this section so one bad copy doesn't kill the loop
  set +e

  for ver in "${VERSIONS[@]}"; do
    src="$OUT_BASE/$ver"
    flat="$OUT_BASE/$ver/_flat"
    mkdir -p "$flat"
    count=0

    if [[ -d "$src" ]]; then
      # Create a stable file list (one path per line, no NULs or process substitution)
      tmplist="$(mktemp)"
      # Only *.cs inside the structured tree
      find "$src" -type f -name '*.cs' > "$tmplist"

      # Read each path safely
      while IFS= read -r file; do
        # Guard: skip empty lines
        [[ -n "$file" ]] || continue

        # Derive assembly = first path segment after "$src/"
        rel="${file#"$src"/}"      # e.g. TaleWorlds.CampaignSystem/Whatever/Foo.cs
        # If the file is directly under $src (unlikely), fall back
        if [[ "$rel" == "$file" ]]; then
          asm="__root__"
        else
          asm="${rel%%/*}"
        fi

        base="$(basename "$file")"
        # Copy; don't abort on failure
        cp -f -- "$file" "$flat/${asm}__${base}" 2>/dev/null || true
        count=$((count + 1))
      done < "$tmplist"

      rm -f -- "$tmplist"
    fi

    echo "Flat mirror for $ver: $flat  ($count files)"
  done

  # Restore strict mode for any code that follows
  set -e
fi
