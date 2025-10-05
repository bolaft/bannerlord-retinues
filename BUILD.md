# Build instructions

## Prerequisites

- **.NET SDK 8.0+** — `dotnet --version` should show 8.x or newer.
- Read access to your Bannerlord installation directory, e.g.  
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`.

> Game assemblies target **.NET Framework 4.7.2**. You don't need to install it separately; the SDK compiles against your local DLL cache.

---

## One‑time setup

1) **Clone** the repository.

2) **Create a local props override** (git‑ignored) to point at your Bannerlord install:

```xml
<!-- Retinues.Local.props -->
<Project>
  <PropertyGroup>
    <BannerlordGameDir>C:\Program Files (x86)\Steam\steamapps\common\Mount &amp; Blade II Bannerlord</BannerlordGameDir>
    <!-- Optional: the default build config when not using --release -->
    <DefaultConfiguration>dev</DefaultConfiguration>
  </PropertyGroup>
</Project>
```

- Keep this file at the repo root.
- Do **not** commit it; it's already in `.gitignore`.

3) **Copy TaleWorld DLLs** into `dll/12/` and `dll/13/`.

4) **Download and extract** Harmony, MCM and UIExtenderEx into the game's Modules folder.

---

## Project layout

```
./build.sh                     # Bash build wrapper (Linux/macOS/Windows Git Bash)
./Directory.Build.Props        # Centralized MSBuild configuration
./Directory.Build.targets      # Centralized staging + deploy targets
./Retinues.Local.props         # (optional) per‑dev overrides (git‑ignored)

./dll/**                       # TaleWorld DLLs
./cfg/core.config.ini          # Copied as config.ini to Retinues.Core module root
./xml/**                       # Copied to Retinues.Core/ModuleData/**
./loc/Languages/**             # Copied to Retinues.Core/ModuleData/Languages/**
./tpl/{templates,partials}/    # Inputs for optional PrefabBuilder (GUI)

./src/Retinues/Core/           # Core module (SubModule.xml here)
./src/Retinues/MCM/            # MCM companion module (optional)
./src/PrefabBuilder/           # Optional GUI generator (Scriban)
```

**Build artifacts**
- Raw build outputs: `bin/build/<Project>/<Config>/...`
- Staged modules: `bin/Modules/<ModuleName>/**`
- Deployed modules (when enabled): `<BannerlordGameDir>/Modules/<ModuleName>/**`

> The deploy step **cleans** the destination module directory by default to avoid stale dev files.

---

## Quick build

### Bash (Linux/macOS or Windows Git Bash)

```bash
# Core module, dev mode (default), deploy into the game's Modules/
./build.sh -t core --deploy
```

**Outputs**
- Staging: `bin/Modules/Retinues.Core/**`
- Deployed: `<BannerlordGameDir>/Modules/Retinues.Core/**`

What gets deployed to **Retinues.Core**:
- `bin/Win64_Shipping_Client/*.dll` (built binaries)
- `SubModule.xml` (from `src/Retinues/Core/`)
- `ModuleData/**` (from `./xml/**`)
- `ModuleData/Languages/**` (from `./loc/Languages/**`)
- `config.ini` (from `./cfg/core.config.ini`, renamed on copy)
- **Dev mode only:** `UIExtenderDebug.xml` (from `src/Retinues/Core/`)

The build system wipes the target module folder before deploy (configurable) so release builds don't keep dev‑only files.

---

## Release builds (with version bump)

Use **`--release <N>`** to:
1) switch to **release** configuration, and
2) set the **last number** in `<Version value="vX.Y.Z.N" />` to `N` in the module’s SubModule XML.

This updates any of these files if present (in `src/Retinues/Core/`):
- `SubModule.BL12.xml`
- `SubModule.BL13.xml`
- `SubModule.xml`

Examples:
```bash
# Release for BL 1.3, set vX.Y.Z.7
./build.sh -t core --release 7 --deploy

# Release for BL 1.2, set vX.Y.Z.8
./build.sh -t core --release 8 --v 12 --deploy
```

> The version edit happens **before** the build so the staged & deployed SubModule use the new value.

---

## Targeting Bannerlord versions

If you support multiple BL versions, select with `-v`:

```bash
# Build against BL 1.3 (default)
./build.sh -t core --deploy -v 13

# Build against BL 1.2
./build.sh -t core --deploy -v 12
```

---

## Prefabs (optional GUI generation)

Generate GUI **prefabs** before building Core:
```bash
./build.sh -t prefabs --prefabs
./build.sh -t core --deploy
```
Prefabs read from `./tpl/templates` + `./tpl/partials` and write to `bin/Modules/Retinues.Core/GUI/**`, which the Core deploy then mirrors to the game folder.

---

## Common commands

Build **everything** (Core + MCM), dev mode, deploy:
```bash
./build.sh -t all --deploy
```

Build **release** (no UIExtenderDebug.xml), bump version patch to `8`, deploy:
```bash
./build.sh -t core --release 8 --deploy
```

Build without deploying (just stage to `bin/Modules`):
```bash
./build.sh -t core --no-deploy
```
