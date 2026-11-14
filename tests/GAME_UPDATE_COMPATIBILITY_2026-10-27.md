# Installed Bannerlord 1.5.6 compatibility audit — 2026-10-27

No new Retinues compatibility defect was found in either working tree. Both the
existing 1.4-target builds and builds compiled against the installed 1.5.6 assemblies
pass the managed checks below. No production code, save schema or release target
was changed for this update.

## Reference and source refresh

- The installed game's `bin/Win64_Shipping_Client/Version.xml` and Native manifest
  identify **1.5.6**. The installation is at
  `C:/Program Files (x86)/Steam/steamapps/common/Mount & Blade II Bannerlord`.
- Updated `dll/15` in both `C:/Users/soufi/Code/_unstable` and
  `C:/Users/soufi/Code/bannerlord-retinues`: **111 game-distributed DLLs**, with
  SHA-256 verification. Retained the six existing mod-dependency references.
  BLSE launcher add-ons found in the game's bin directory are excluded.
- Refreshed `src/TaleWorlds/15` in both checkouts: **76 managed game assemblies,
  7,763 C# files**, decompiled using ILSpy 10.1.0.8386. Existing unrelated
  dependency-source directories in the stable checkout were preserved.
- Both refreshed directories contain `REFERENCE_SNAPSHOT.json`, recording version,
  source paths, hashes and decompiler results. These directories remain Git-ignored.
- Backed up the old DLL sets and replaced source directories under
  `C:/Users/soufi/Code/_unstable/out/game-update-audit/20261027/before/`.
- All original `dll/14` hashes were verified unchanged. The official release target
  remains 1.4; the beta remains a compatible runtime, not a separate manifest.

Each original reference set had 109 DLLs: 103 matching game-distributed filenames
and six mod dependencies. Of those 103 game files, 102 hashes changed and one was
identical. Hash changes alone do not imply incompatible APIs. The metadata/IL audit
compared 68 managed assemblies against both the old beta references and the prior
installed 1.5.4 snapshot, then checked the APIs actually used by Retinues.

## Validation against installed 1.5.6

The two reference profiles use the same `BL14` compilation symbol as the current
1.4/1.5 release. One uses the preserved 1.4 references; the other uses fresh 1.5.6
references. Every runtime check loads the installed 1.5.6 game assemblies.

| Check, per reference profile | Stable | V2 |
| --- | ---: | ---: |
| Debug and Release builds | Pass, no warnings | Pass, no warnings |
| Headless test executions | 53 / 53 | 116 / 116 |
| Direct game type references resolved | 247 / 247 | 321 / 321 |
| Direct game member references resolved | 703 / 703 | 926 / 926 |
| Annotated Harmony containers processed | 39 / 39 | 57 / 57 |
| Harmony targets installed | 44 | 71 |
| Release test-code exclusion | Pass | Pass |
| Prior stable save contract (`ff69d116`) | Pass | Pass |
| Prior V2 save contract (`96a9dc90`) | N/A | Pass |

All **30 build/validation processes passed**, including **338 headless test
executions** across the four branch/reference combinations, with no failures or
skips. These include repeated coverage across builds, not 338 distinct features.
Stable's event-only `VolunteerSwapForPlayer` container has no Harmony target;
this is expected. Patch checks install actual Harmony patches, including parameter
and private-field binding and transpiler processing, then unpatch each container.

The native prefab XML also resolves all **20 stable and five V2 extension targets**.
Stable's whole-document finance-panel patch correctly tolerates the absent legacy
`FinancePanelWidget`; it does not require that widget to exist.

## Changes reviewed

- `PartyBase.OnXpChanged` and `TroopRoster.AddXpToTroopAtIndex` have no reported
  signature or symbolic-IL changes from the previous installed 1.5.4 snapshot.
- `MobilePartyHelper.CanTroopGainXp` and `PartyScreenHelper.ClosePartyPresentation`
  differ only in embedded assertion source-line numbers in the reviewed methods.
- `DefaultPartyTrainingModel.GetEffectiveDailyExperience` switches its perk queries
  to an overload without the unused `out Hero`. Stable's diagnostic finalizer still
  binds; it does not depend on the method's instruction sequence.
- `TooltipRefresherCollection` adds tooltip hint/parley support elsewhere in the
  class. `RefreshItemTooltip` decompiles identically; its symbolic-IL difference is
  due to renumbered generated lambdas. V2's postfix signature remains valid.
- The earlier `TooltipProperty` constructor workaround still resolves and passes
  the engine tooltip probes on 1.5.6.

## Evidence and limits

Local evidence: `C:/Users/soufi/Code/_unstable/out/game-update-audit/20261027/`:
`validation-summary.json`, branch/profile JUnit reports, reference and save-contract
XML reports, `prefab-checks.json`, DLL inventories, source manifests and the comparison
reports. `Collect-Installed.ps1`, `Decompile-Installed.ps1`, `Validate-Installed.ps1`
and `Check-Prefabs.ps1` record the audit procedure. Collection/refresh scripts are
one-shot snapshot operations; do not rerun them over the original backup.

The test host uses the complete installed Harmony dependency directory. Builds use
the existing user NuGet package cache and isolated output; deployment is disabled.
No installed game files, deployed Retinues build, saves or release ZIPs were changed.

This is managed compatibility and headless regression evidence. It does not execute
native rendering/combat, other mods' patch interactions, or a full campaign save/load
in the running game. Live smoke testing should cover editor/tooltip opening, a daily
training tick, recruitment, save/reload and a battle before publishing a new release.
