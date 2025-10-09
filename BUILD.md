# Build instructions

## Prerequisites

- **.NET SDK 8.0+** - `dotnet --version` should show 8.x or newer.
- Read access to your Bannerlord installation directory (e.g.  
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`).

> Game assemblies target **.NET Framework 4.7.2**. You don't need to install it; compilation uses the game DLLs from your local install or cache.

---

## One‑time setup

1) **Clone** the repository.

2) **Create a local props override** (git‑ignored) to point at your Bannerlord install (optional if you pass `--game-dir` each time):

```xml
<!-- Retinues.Local.props -->
<Project>
  <PropertyGroup>
    <BannerlordGameDir>C:\Program Files (x86)\Steam\steamapps\common\Mount &amp; Blade II Bannerlord</BannerlordGameDir>
  </PropertyGroup>
</Project>
```

- Keep this file at the repo root.
- Do **not** commit it; it's already in `.gitignore`.

3) Copy the game's DLLs to the local cache:
```
dll/12/**   # BL 1.2 DLL set
dll/13/**   # BL 1.3 DLL set
```
The build picks the correct set based on `--version 12|13` (defaults to 13).

4) Download Harmony, UIExtenderEx and MCM and extract them in the game's Modules folder.
---

## Project layout

```
build.sh                      # Build wrapper (Git Bash/WSL/macOS/Linux)
Directory.Build.props         # Centralized MSBuild configuration
Directory.Build.targets       # Staging + deploy targets
Retinues.Local.props          # (optional) local overrides (git‑ignored)

cfg/core.config.ini           # -> Retinues/config.ini (on deploy)
xml/**                        # -> Retinues/ModuleData/**
loc/Languages/**              # -> Retinues/ModuleData/Languages/**
tpl/{templates,partials}/     # Inputs for PrefabBuilder (GUI)

src/Retinues/Retinues.csproj  # The game module project
src/Retinues/SubModule*.xml   # SubModule files (BL‑specific)
src/Retinues/UIExtenderDebug.xml  # Copied only in non‑Release builds
src/PrefabBuilder/PrefabBuilder.csproj # GUI generator tool (not deployed)
```

**Artifacts**
- Raw project outputs: `bin/build/<Project>/<Config>/<TFM>/...`
- **Staging** (what gets deployed): `bin/Modules/Retinues/**`
- **Game deploy**: `<BannerlordGameDir>/Modules/Retinues/**`

> Deploy cleans the game module directory first while **keeping `*.log` files** by default.

---

## Quick builds

### Dev (Debug), deploy to game
```bash
./build.sh --deploy
```
- Copies **UIExtenderDebug.xml** to the module root.
- Generates GUI prefabs (if PrefabBuilder project exists) and deploys `GUI/**`.
- Deploys `Retinues.dll`, `SubModule.xml`, `ModuleData/**`, `config.ini`.

### Prefabs only (plus GUI deploy)
```bash
./build.sh --prefabs --deploy
```
- Runs PrefabBuilder to `bin/Modules/Retinues/GUI/**`.
- Mirrors `GUI/**` to `<Game>/Modules/Retinues/GUI/**`.

### Release build with version bump
```bash
# Set the last number in <Version value="vX.Y.Z.N" /> to 12 and build Release
./build.sh --release 12 --deploy
```
- Updates `src/Retinues/SubModule.BL12.xml`, `src/Retinues/SubModule.BL13.xml`, and/or `src/Retinues/SubModule.xml` if present.
- Builds **Release** (no `UIExtenderDebug.xml`).
- Deploys to the game folder.

### Target a specific Bannerlord version
```bash
# BL 1.3 (default)
./build.sh --deploy --version 13

# BL 1.2
./build.sh --deploy --version 12
```

You can also force the game path inline:
```bash
./build.sh --deploy --game-dir "D:\Steam\steamapps\common\Mount & Blade II Bannerlord"
```
