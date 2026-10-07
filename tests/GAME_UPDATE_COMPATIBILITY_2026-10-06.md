# Installed Bannerlord 1.5.4 compatibility audit — 2026-10-06

The installed update contains one confirmed binary API break used by both Retinues branches:
the text `TooltipProperty` constructor. It was reproduced, fixed in both working trees and
covered by regression tests. No other unresolved direct game references or failed annotated
Harmony patch installations remained in the tested builds.

The DLL currently installed in `Modules/Retinues/bin/Win64_Shipping_Client/Retinues.dll`
still contains the incompatible call. **The fixed builds have not been deployed.**

## Inputs and scope

- Installed game: `C:/Program Files (x86)/Steam/steamapps/common/Mount & Blade II Bannerlord`.
  `bin/Win64_Shipping_Client/Version.xml`, `Modules/Native/SubModule.xml` and
  `Modules/SandBox/SubModule.xml` identify version **1.5.4**.
- Stable checkout: `C:/Users/soufi/Code/bannerlord-retinues`, branch `main`.
- V2 checkout: `C:/Users/soufi/Code/_unstable`, branch `unstable`.
- Tested the current working trees, including their existing uncommitted fixes. Stored
  `dll/14` and `dll/15` sets match byte for byte between the two repositories.
- Each set contains 109 DLLs: 102 differ from the matching installed files and seven match.
  None is missing after resolving official-module and dependency-module paths. A different
  hash does not itself establish an API break; many game assembly versions remain `1.0.0.0`.
- Staged 81 installed managed assemblies in ignored output for isolated checks. Compared
  metadata and symbolic IL for 68 official managed game assemblies, then checked the APIs
  actually referenced and patched by Retinues. Native DLL implementation changes were not audited.
- Reference snapshots in `dll/` were preserved. No game files, user saves or deployed mods
  were changed, and Bannerlord was not launched for this pass.

## Confirmed tooltip failure and fix

The stored references expose a five-argument constructor:

```text
TooltipProperty(string, string, int, bool, TooltipPropertyFlags)
```

The installed game instead exposes a six-argument constructor with an optional trait list.
Optional arguments allow source code to compile but do not preserve the old binary signature.
An existing DLL built against the old references therefore throws `MissingMethodException`
when it constructs these tooltip rows.

Before the fix, the new V2 tests reproduced this in both the editor tooltip builder and the
unlocked-items scoreboard tooltip builder: 40 headless cases passed and these two failed.
The stable binary and currently installed mod also reference the missing constructor.

Both branches now build text rows with the existing parameterless constructor and public
property setters. The resulting title, text, height, visibility and modifier values are
preserved, without choosing a constructor signature tied to a particular game version.
The tests invoke the actual tooltip builders, including Unicode text and the unlock-list
overflow row. No save types, field IDs, container registrations or persistence keys changed
as part of this fix.

## Results

| Check against installed 1.5.4 | Stable | V2 |
| --- | ---: | ---: |
| Direct game type references resolved, Release built with stored 1.4 references | 235 / 235 | 295 / 295 |
| Direct game member references resolved, same binaries | 683 / 683 | 830 / 830 |
| Annotated Harmony containers processed without failure | 35 | 51 |
| Actual Harmony targets installed | 39 | 64 |

Stable has one annotated container, `VolunteerSwapForPlayer`, that only wires an event and
has no Harmony method to install; the other 34 install patches. V2 has no empty containers.
The installation probe exercises Harmony's actual binding, parameter/private-field injection
and transpiler processing. Each container is unpatched afterward to isolate subsequent probes;
this does not test interactions with other mods or execute every patched game path.

The installed-game matrix tested V2 built with stored 1.4 references, stored beta references
and installed 1.5.4 references, plus stable built with stored 1.4 and installed 1.5.4 references:

- **38 / 38 processes passed**: ten Debug/Release mod builds, five runtime runs, five Release
  test-exclusion checks, five direct-reference checks, five Harmony installation runs and
  eight prior-save contract comparisons.
- **497 passing headless executions**: 486 seeded/repeated case executions plus 11 supplemental
  engine checks; no failures. These are repetitions and build combinations, not 497 unique features.
- All eight save comparisons passed against the prior stable (`ff69d116`) and V2 (`96a9dc90`)
  contracts as applicable. These compare registration/schema and exercise managed model data;
  they do not replace loading and resaving a complete campaign `.sav` in the game.

The normal supported-version matrix was then rerun with the fix:

- **41 / 41 processes passed**, including 14 Debug/Release mod builds and 11 prior-save comparisons.
- **807 passing headless executions**: 789 repeated cases and 18 supplemental engine checks,
  spanning stable 1.2/1.3/1.4 and V2 1.2/1.3/1.4/stored beta, including a 1.4 build on beta DLLs.

Changed game code relevant to the mod was also inspected. The changes in the patched
`MobilePartyHelper.CanTroopGainXp` and `PartyScreenHelper.ClosePartyPresentation` methods
were assertion source-line values, with the same logic and signatures. The tooltip view model
uses the new constructor internally. `ButtonWidget.HandleClick` now ignores disabled buttons.
Save-definition assembly discovery uses a safer referenced-assembly enumerator, and XML loading
adds line-aware diagnostics. No additional Retinues incompatibility was identified in these
paths. The engine also changes asynchronous save-file handling, which these managed probes
do not establish is correct for a complete campaign save.

## Evidence and reusable checks

Local evidence and exact tested binaries are under `out/game-update-audit/20261006/`:

- `dll-inventory.json`, `managed-inventory.json`: source paths, hashes and inventory.
- `api-14-to-installed.xml`, `api-15-to-installed.xml`: managed metadata/IL changes.
- `tooltip-before.xml`: the two reproduced constructor failures.
- `installed-retinues-references.xml`: the still-installed mod's unresolved constructor.
- `fixed/validation-summary.json` and adjacent XML/logs: installed-game checks.
- `supported-matrix/summary.json` and adjacent XML/logs: supported-version checks.
- `fixed/binaries/v2-14-Release/Retinues.dll` and
  `fixed/binaries/stable-14-Release/Retinues.dll`: tested compatible Release assemblies.

The new `tests/GameAssemblyAudit` tool reads metadata without executing game code. Its local
Mono.Cecil reference defaults to version 0.11.4 in the NuGet cache; set `CecilPath` to an existing
compatible DLL when that cache entry is absent. The headless runner accepts `--patch-check`.
Example commands from the V2 checkout, using the installed snapshot collected for this audit:

```powershell
$auditRoot = 'C:/Users/soufi/Code/_unstable/out/game-update-audit/20261006'
$installed = "$auditRoot/installed-managed"
$module = "$auditRoot/fixed/binaries/v2-14-Release/Retinues.dll"
& "$auditRoot/auditor/GameAssemblyAudit.exe" diff ./dll/15 $installed "$auditRoot/api-15-to-installed.xml"
& "$auditRoot/auditor/GameAssemblyAudit.exe" refs $module $installed "$auditRoot/v2-references.xml"
& "$auditRoot/runner/HeadlessAudit.exe" $module $installed v2 $installed --patch-check "--junit=$auditRoot/v2-patches.xml"
./tests/Run-Validation.ps1 -NoRestore -Seed 20261006 -Repeat 3 -OutputDirectory out/game-update-audit/20261006/supported-matrix
```

Supply a complete matching managed assembly directory, including official UI modules and
dependencies, to avoid false missing-target reports. A future update needs a fresh snapshot.
Direct-reference resolution excludes reflection-only lookups; the patch and runtime probes
cover additional paths but cannot prove that every UI, mission, campaign or mod combination
works. Full save/load/restart and live-game acceptance remain the user's pending checks in
[LIVE_GAME_MATRIX.md](LIVE_GAME_MATRIX.md); the earlier known live-test limitations in
[VALIDATION.md](VALIDATION.md) still apply.
