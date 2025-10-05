# Build instructions

## Prerequisites

- **.NET SDK 8.0+** - `dotnet --version` should show 8.x or newer.
- Read access to your Bannerlord installation directory, e.g.  
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord`.

> Game assemblies target **.NET Framework 4.7.2**. You don't need to install it separately on the build machine; the SDK compiles against the referenced game DLLs via `BannerlordGameDir`.

---

## One‑time setup

1) **Clone** the repository.

2) **Create a local props override** (git‑ignored) to point at your Bannerlord install:

```xml
<!-- Retinues.Local.props -->
<Project>
  <PropertyGroup>
    <BannerlordGameDir>C:\Program Files (x86)\Steam\steamapps\common\Mount &amp; Blade II Bannerlord</BannerlordGameDir>
    <!-- Optional: the default build config when you omit -c/‑Config -->
    <DefaultConfiguration>dev</DefaultConfiguration>
  </PropertyGroup>
</Project>
```

- Keep this file at the repo root.
- Do **not** commit it; it's already in `.gitignore`.
- You can override any property at build time with `-p:Name=Value`.

---

## Project layout (relevant to build)

```
./build.sh                     # Bash build wrapper (Linux/macOS/Windows Git Bash)
./Directory.Build.Props        # Centralized MSBuild configuration
./Directory.Build.targets      # Centralized staging + deploy targets
./Retinues.Local.props         # (optional) per‑dev overrides (git‑ignored)

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
# Core module, dev mode, deploy into the game's Modules/
./build.sh -t core -c dev --deploy
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

## Common commands

Build **everything** (Core + MCM), dev mode, deploy:
```bash
./build.sh -t all -c dev --deploy
```

Build **release** (no UIExtenderDebug.xml), deploy:
```bash
./build.sh -t core -c release --deploy
```

Build without deploying (just stage to `bin/Modules`):
```bash
./build.sh -t core --no-deploy
```

Override the game directory on the fly:
```bash
./build.sh -t core -g "D:\Games\Bannerlord"
```

(Optional) Generate GUI **prefabs** before building Core:
```bash
./build.sh -t prefabs --prefabs
./build.sh -t core --deploy
```
Prefabs read from `./tpl/templates` + `./tpl/partials` and write to `bin/Modules/Retinues.Core/GUI/**`, which the Core deploy then mirrors to the game folder.
