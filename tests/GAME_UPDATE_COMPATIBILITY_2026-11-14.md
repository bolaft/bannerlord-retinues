# Installed Bannerlord 1.5.7 compatibility audit — 2026-11-14

No new Retinues compatibility defect was found in either branch. The current
BL1.4-target release binaries and isolated builds using the installed 1.5.7
references pass the managed checks below. No production code or save schema
change was required.

## Installed versions and refreshed references

- The installed `Version.xml` and base-game manifests identify **Bannerlord 1.5.7**.
  `NavalDLC/SubModule.xml` identifies **War Sails 1.3.7**. These are the base game
  and expansion versions, respectively, not two Bannerlord branch versions.
  The [official November 11 announcement](https://steamcommunity.com/app/261550?snr=2_groupannouncements_detail_)
  states that these versions are live on PC.
- Refreshed `dll/15` in both `_unstable` and `bannerlord-retinues` with the installed
  **111 game-distributed DLLs**, verified by SHA-256. Compared with the previous
  1.5.6 snapshots, 110 changed and one was identical. Retained the six third-party
  mod dependency references; excluded BLSE launcher add-ons.
- Refreshed `src/TaleWorlds/15` in both checkouts: **76 managed game assemblies,
  7,763 C# files** decompiled by ILSpy. Copied source files are hash-verified too.
  Unrelated dependency-source directories remain in place.
- Each refreshed directory contains a `REFERENCE_SNAPSHOT.json` manifest with
  version, source paths, hashes and decompiler results. These remain Git-ignored
  local reference caches.
- Previous references and replaced source directories are backed up under
  `out/game-update-audit/20261114/before/`. The original `dll/14` hashes and the
  third-party dependencies were verified unchanged; `dll/12` was not modified.

## Validation

The `14` profile uses the exact Debug/Release binaries built earlier on November 14,
after verifying that their recorded source inputs still match the working trees.
The release DLLs are the ones packaged in `Retinues_v1.4.14.34.zip` and
`Retinues_v2.0.0.15.zip`. The `157` profile rebuilds isolated copies against the
fresh game DLLs, retaining the supported `BL14` compilation symbol. Every runtime
check below uses the installed **1.5.7** assemblies.

| Check, per reference profile | Stable | V2 |
| --- | ---: | ---: |
| Fresh 1.5.7 Debug and Release builds | Pass, no warnings | Pass, no warnings |
| Headless test executions | 62 / 62 | 134 / 134 |
| Direct game type references resolved | 247 / 247 | 325 / 325 |
| Direct game member references resolved | 709 / 709 | 943 / 943 |
| Annotated Harmony containers processed | 40 / 40 | 58 / 58 |
| Harmony targets installed | 45 | 73 |
| Release test-code exclusion | Pass | Pass |
| Prior stable save contract (`ff69d116`) | Pass | Pass |
| Prior V2 save contract (`96a9dc90`) | N/A | Pass |

All **28 build/validation processes passed**, including **392 headless test
executions** across the four branch/reference combinations, with no failures or
skips. These are repeated executions, not 392 distinct features. The Harmony
checks install actual patches, including parameter/private-field binding and
transpiler processing, then unpatch each container. Stable's event-only volunteer
swap container having no patch target is expected.

The installed native prefab XML resolves all **20 stable and five V2 extension
targets**. The absent legacy finance widget remains optional for the stable
whole-document patch.

## Relevant game changes reviewed

The metadata/symbolic-IL comparison covers 71 game assemblies: 65 removed members
or types, 81 additions, 87 metadata changes, and 106 changed method bodies. Many
entries are compiler-generated members or native network callback changes;
these totals do not represent Retinues incompatibilities. The refreshed source
snapshot also includes the five StoryMode assemblies. Across all 76 assemblies,
77 decompiled C# files changed, with no source files added or removed. The
StoryMode change is its party-size model override, reviewed with the base model.

- Two patched methods, `MobilePartyHelper.CanTroopGainXp` and
  `PartyScreenHelper.ClosePartyPresentation`, differ only in embedded assertion
  source-line numbers in the reviewed code.
- `GetPartyMemberSizeLimit` adds a required `isCurrentlyAtSea` argument, including
  the StoryMode override. `PartyBase.PartySizeLimit` now supplies that state.
  Neither Retinues branch directly calls or overrides the changed model method;
  both already read the party property, so recruitment limits follow the corrected
  native result without an adapter.
- `PartyScreenLogic.TransferTroop` now subtracts transferred XP from both source
  sides. V2 already ignores nonpositive XP grants and ignores positive XP
  relocation while a `PartyState` is active. This native fix does not require
  changing the skill-point hook or dynamic retinue-upgrade handling.
- The `OnTroopScoreHit` call chain now forwards a captain through the supplier,
  map-event side and map-event party into `CombatXpModel.GetXpFromHit`. Neither
  Retinues branch calls or patches those changed signatures. V2 captures the
  resulting awarded XP through the unchanged roster API.
- `TroopRoster`, `MobilePartyTrainingBehavior` and
  `GarrisonRecruitmentCampaignBehavior` decompile identically to 1.5.6.
  `PartyBase.OnXpChanged` has no signature or symbolic-IL change. The existing
  depleted-row training safeguards still pass their integration checks.
- `BasicCharacterTableau` adds validation for unavailable race/mount resources,
  relevant to the native save/load thumbnail crash fix. No consumed Retinues
  member was removed or changed incompatibly.

## Evidence and limits

Evidence and reproducible scripts are in
`C:/Users/soufi/Code/_unstable/out/game-update-audit/20261114/`:
`installed-inventory.json`, `reference-comparison.json`,
`api-1.5.6-to-1.5.7.xml`, `affected-retinues-members.json`,
`relevant-source-diffs.patch`, `decompiled-source-comparison.json`,
`validation-summary.json`, JUnit/reference/save-contract reports,
`prefab-checks.json`, and both `refresh-*.json` manifests.

The collection and refresh scripts are one-shot operations that preserve their
original backups. Builds and tests use isolated output with deployment disabled.
The three release ZIPs in Modules were verified against their earlier package
hashes; no release ZIP, installed module or save was replaced during this audit.

These checks exercise managed code and isolated fixtures. They do not run native
rendering/combat or a complete campaign in Bannerlord, and do not guarantee
compatibility with every third-party patch combination. Live smoke testing can
cover editor/tooltips, troop transfers, daily training, recruitment, a battle and
save/reload on 1.5.7.
