# Validation evidence — 2026-09-09

## V2 tier-setting visibility — 2026-11-14

Skill caps and totals now both hide tiers above Maximum Troop Tier. The expanded
settings test checks all 22 controls as the maximum increases and decreases from
6 through 10, including preservation of the configured values while hidden.

`out/release-tier-visibility-20261114/build_release.py unstable 15` built isolated
Debug and Release snapshots with no warnings or errors. Both BL14 and BL15
managed runs passed all 134 cases; Release test exclusion, patch bindings, game
references, and prior stable/V2 save contracts passed. The refreshed archive
targets Bannerlord 1.5.7 and changes only `Retinues.dll` from the preceding ZIP.
The previous scrollbar removal is preserved. Native in-game rendering was not
retested; the checks exercise the actual settings view models and dependencies.

## Installed Bannerlord 1.5.7 — 2026-11-14

See [the compatibility audit](GAME_UPDATE_COMPATIBILITY_2026-11-14.md). Both
branches pass against the installed 1.5.7 game, using the current release
binaries and fresh builds against 1.5.7 references: 392 headless test executions,
all consumed game members resolved, Harmony installation and save-contract
checks passed, and all 25 native prefab extension targets resolved. Updated
both checkouts' `dll/15` and `src/TaleWorlds/15` caches with verified copies and
backups. No production-code change was required; live game testing remains
separate from this managed compatibility audit.

## Nord castle-village elite recruitment report — 2026-11-14

Not reproduced in the isolated code checks; no recruitment change made.
The installed SandBoxCore culture XML gives the placeholder Nord culture Sturgian
regular/elite roots. Applying the installed War Sails culture XSLT changes them
to `nord_youngling` and `nord_ungmann`. Sturgian volunteers alone therefore do not
establish that Retinues selected the player's culture.

The probe in `out/nord-recruitment-20261114/` reconstructs Nord and Sturgian troop
trees from the installed NPC XML (including partial DLC overrides), with a custom
copy of the Sturgian trees. Against BL14, both current branches match all 24
Nord regular/elite and Sturgian elite candidates to the expected custom tree.
Stable's actual `WNotable.SwapVolunteers` also replaces every tested elite slot,
replaces the regular slot and preserves an empty slot. The stable recruitment
sources used by that binary match the current checkout. V2 checks its volunteer
matcher with canonical tree-role flags supplied by the fixture.

These checks use isolated campaign objects and synthetic equipment/skills;
they do not load the reporter's save, initialize the full recruitment screen or
reproduce other mods' patches. The exact game/mod versions, mod list, debug log
and a save near an affected village are needed to diagnose the remaining report.

## V2 character panel scrolling reverted — 2026-11-14

Reverted the entire character-panel scrolling change at the user's request after
live visual feedback. Restored the original template from
`out/character-scroll-20261114/character-before.j2`, including its original size
policies and alignment. The earlier scroll-layout probe results describe the
reverted implementation, not the current editor. The overflow report is no longer
claimed as fixed in the changelog.

Both editor prefab variants were regenerated and XML-validated. The current
character panel matches the original snapshot; only the two editor XML entries
changed in the refreshed V2 ZIP. The previously validated release DLL and all
other payload files remain byte-identical. Evidence:
`out/release-v2-no-scroll-20261114/verification.json`.

## V2 ultrawide equipment layout — 2026-11-14

The troop preview is anchored from the left, while the equipment panel was
anchored to the right edge. The panel now removes only the viewport width beyond
16:9, accounting for Gauntlet's UI scale. Only the equipment page opts in; page
changes and window resizing restore the original prefab margin. Nominal 16:9
modes with pixel rounding (1366x768) are also left unchanged.

Four regressions exercise the widget's update callback and Gauntlet's real
measure/layout with an isolated viewport and no graphics initialization. Panel
and child positions/sizes match the previous layout exactly at 1280x720,
1366x768, 1920x1080, 2560x1440, 3840x2160 and narrower aspect ratios, at 75%,
100% and 125% UI scale. Ultrawide checks cover 2560x1080, 3440x1440, 3840x1600
and 5120x1440, child containment, repeated updates, page changes and resizing.
All other panel viewmodels keep the correction disabled.

Both BL14 and BL15 full suites pass: 126 test cases plus 8 integration checks
per runtime, with no failures or skips (212 new layout assertions per runtime).
All seven prefabs parse for both targets; the editor XML differs only in the
panel widget type and its opt-in binding. Evidence: `out/ultrawide-layout-20261114/`.
This validates managed layout geometry, not a live ultrawide game session.

## Elite promotions and stalled doctrine reports — 2026-11-14

No current counting failure reproduced in the ordinary promotion paths. Stable's
new `EliteFeatTests` runs the actual upgrade dispatcher, troop classification,
doctrine prerequisite filtering and progress service. An isolated service replaces
the campaign lookup. Native terminal T6 promotions count in batches, T6 stops
counting when a T7 upgrade is attached, terminal T7 and custom elite troops count,
and the feat completes at 100. It remains inactive before Steadfast Soldiers is
acquired, as intended. This is the existing stable behavior, not a new fix.

V2's test uses the actual elite-promotion handler, activation gate, registry and
doctrine behavior in an isolated campaign. Troop membership/source caches are
fixtures. A 60 + 40 batch awards 20 doctrine progress and resets the repeatable
feat; a saved completion parked at 100 recovers on the next promotion. A regular
source troop and an unmet prerequisite are rejected. The prior-save repeatable
feat recovery was already included in commit `7af9cf41` (v2.0.0.13).

The reported V2 percentages alone are inconclusive: either Peak Performance or
Public Safety awards 35 points to Steadfast Soldiers; Watchers on the Walls awards
55 to Stalwart Militia. The remaining siege feats have additional conditions.
They Shall Not Pass currently uses the best single siege-defense kill count,
not cumulative kills, and the common battle hook excludes simulated battles.
Do not claim these reports are resolved without a version and the unmet feat
details or a reproducible save.

Full suites pass: 62 stable checks on each of BL12/14/15 and 130 V2 checks on each
of BL14/15, 446 executions with no failures or skips. Debug builds are clean.
Evidence: `out/feat-progress-20261114/`. These are managed engine tests, not live
campaign promotion or siege tests. Only tests/documentation were added; no
production code, save schema, changelog, ZIP or installed module changed.

## Stable formation choices after reload — 2026-11-14

Stable stored the manual formation override on each short-lived `WCharacter`
wrapper. Fresh wrappers used by recursive `TroopSaveData` capture therefore
saved `Unset` instead of the player's choice. The option gate also discarded
the saved choice when overrides were disabled. Stable now shares choices by
troop ID, gates their application rather than storage, and clears the transient
map at campaign start and when a custom slot is initialized or removed. The
existing `TroopSaveData` field 12 remains unchanged.

Four headless regressions fail against the pre-fix stable code and pass with
the fix: fresh wrappers, a bow-equipped infantry override in an upgrade tree,
temporarily disabled overrides, and slot/campaign cleanup (16 assertions per
engine). The in-game sandbox now restores this new transient map too. An
additional campaign save/restore regression was added but was not run live.

V2 needs no production change: a separate regression preserves a bow-equipped
troop's infantry choice and derived flags through three serialized reloads,
then checks that Auto restores ranged classification (18 assertions).

Both branches' full headless suites pass on BL12, BL14 and BL15: 61 stable and
129 V2 checks per version, 570 executions with no failures or skips. Stable
Debug/Release BL12/14 and V2 Debug BL12/14 builds pass without warnings/errors.
Stable's Release save contract remains compatible with the prior-release
fixture; Release excludes test code. Evidence: `out/formation-save-20261114/`.
These checks use managed game assemblies, not a live campaign playthrough.
No release ZIP or installed game module was changed.

## Hideout skill XP and repeated awards — 2026-11-14

Confirmed two V2 problems consistent with the report. Bannerlord's
`BattleCampaignBehavior.OnHeroCombatHit` grants Baptised in Blood XP to every
infantry soldier in the party, including hideout reserves. V2 previously converted
all of it into skill-point XP. In BL14/15, `AddXpToTroopAtIndex` also adds the
stack's existing XP to its `xpAmount` argument; reading that argument in the
postfix counted the stored XP again on subsequent awards.

V2 now snapshots each new grant before the original method changes it, after
ordinary multiplier prefixes. Scoped perk/shared-party awards in hideouts count
only allocated, surviving soldiers from the receiving party. Direct combat XP,
ordinary battle sharing, the original roster XP awards and existing saved points
are preserved. Scope cleanup runs on exceptions and restores nested contexts.
Stable uses separate mission kill tracking and has neither of these XP hooks;
no stable production changes were needed.

`HideoutXpTests` invokes the real engine perk callback, shared-XP helper and roster
methods with the actual Retinues patches installed. Only the final skill-point
converter is intercepted to observe its input without invoking native UI. Cases
cover 100 reserves with none/three/four deployed, casualties, another party's
matching troop, direct combat, field battles, simulation, nested/error cleanup,
zero/negative grants, capped stacks and a separate XP multiplier prefix. The test
also exposed the repeated-XP issue: the second 500-XP perk award credited twice
the expected participating share before the snapshot fix.

All four regressions (25 assertions per engine) and full headless suites pass on
BL12, BL14 and the BL14 binary running on BL15: 384 total test executions, no
failures or skips. Evidence: `out/hideout-xp-20261114/`. These are managed engine
tests, not a live hideout playthrough. No save schema, released ZIP or installed
game module was changed.
Debug BL12/14 and Release BL14 builds pass without warnings/errors. Release
excludes tests and fixtures; all 58 Harmony containers bind on BL14 and BL15
(73 installed targets, zero failures).

## Indomitable team eligibility — 2026-11-14

Confirmed the report in V2: `RetinueHealthBonusPatches` checked the player's acquired
doctrine and the troop's retinue flag, but not its team. Stable already checks the
agent's team against the main agent's team in `IndomitableBehavior.OnAgentCreated`
and applies a flat +5 HP, so the reported +10% enemy bonus did not affect stable.

V2 now adds Indomitable only when the spawned agent belongs to the mission's
non-null player team. Using `Mission.PlayerTeam` avoids depending on the main
agent having spawned first. The configurable base retinue health bonus still
applies to retinues on either side. No save data or schema changed.

The new regression exercises the production bonus calculation with real retinue
wrappers, the same troop on opposing teams, missing/unassigned teams, normal
troops, acquired/unacquired doctrine, zero configured bonus and percentage rounding.
Before the fix, the enemy case failed with expected bonus 25 / actual 35.
Afterward, all 13 assertions pass on BL12, BL14 and the BL14 binary on BL15.
The three headless suites pass 372 total test executions with no failures/skips.
Debug/Release builds pass; Release excludes tests and all 57 Harmony containers
bind on BL14/15 (71 installed targets). Evidence: `out/indomitable-20261114/`.

## Released-save preservation — 2026-11-02

Audited the three November 1 ZIPs against eleven older release binaries, including
the original published `.34` and `.15` files. Found and fixed captain back-links
redirecting stable-save conversion/import writes into the base troop. New tests
exercise both conversion orders, already-linked captains, the real migration and
import paths, distinct XP pools, and a subsequent model reload.

All 40 final forward save-contract comparisons pass. All 17 same-branch XML object
comparisons preserve the previous definitions. The full matrix passed 41 processes
and 2,251 test executions; the later focused pass with an additional importer test
passed 38 processes and 1,932 executions, with zero failures/skips. Actual ATC and
Improved Garrisons probes also pass on BL14/15. The rebuilt V2 ZIP in `Modules`
contains the fix; stable packages remain as built on November 1.

See `tests/SAVE_COMPATIBILITY.md` for scope, hashes and limits, especially the
pre-existing requirement to bridge very old `ret_` saves through stable before V2.
Evidence: `out/save-audit-20261102/` and `out/releases-20261102/`.

## Adonnay's Troop Changer recruitment — 2026-11-01

Both stable and V2 now recognize ATC's configured basic/elite volunteer trees in
recruitment only. The adapter reads the active mod's loaded config, including
clan/faction/culture precedence and recruiter substitutions, without a compile-time
ATC dependency. Ambiguous roles are left alone. Culture trees, militia rosters,
saved troop identities and save definitions are not modified.

Evidence: `out/atc-audit-20261101/final-matrix/summary.json`: all 46 processes pass,
including Debug/Release on BL12/13/14/15, release test exclusion and 12 prior-save
contract checks. Including both BL14 builds running on BL15, the ten headless suites
execute 885 tests, with zero failures or skips. Six Harmony binding checks pass for
both branches on BL12, BL14 and the BL14 binaries on BL15.

`ATCRecruitmentProbe` passes on BL14 and BL15 using the downloaded ATC 1.12.0 DLL
(SHA-256 `01AD0BF21F1ACD0942EBB388F1062D49468F3FE28F659EE9C52F516EAFB33F98`).
It exercises actual ATC configuration/selection, V2's active-model wrapper, player
pool replacement, and ATC's AI prefix followed by Retinues' transfer hook. Checks
include empty/purchased slots, role overlap with militia, disabled trees, culture
and recruiter restrictions, prepared uninstall, and ATC absent/disabled. Stable
also repairs old militia recruitment offers only with a proven original ATC source;
missing origins are left alone and unavailable custom roots restore that original.
The existing downloaded Improved Garrisons integration probe also passes.

Native campaign state, MCM settings access, elite random choice and the final native
payment/roster boundary are fixtures. No live DRM campaign, deployment or real save
load was performed. Live checks are listed in `LIVE_GAME_MATRIX.md`; the downloaded
third-party DLL is not part of the repository or releases.

## Installed Bannerlord 1.5.6 — 2026-10-27

See [the compatibility audit](GAME_UPDATE_COMPATIBILITY_2026-10-27.md). Both branches
pass against installed 1.5.6 with preserved 1.4 references and fresh 1.5.6 references:
30 build/validation processes, 338 headless executions, no failures/skips, all direct
game references and annotated Harmony patches resolved, and prior-save contracts
preserved. Native prefab extension targets also resolve. Beta DLLs and decompiled
sources were refreshed in both checkouts; no production code change was needed.
This pass did not launch the game or deploy a build.

## Rescue Mission battles and prison breaks (V2) — 2026-10-23

Evidence: `out/rescue-mission-20261023/validation/summary.json` and the five Debug
JUnit reports. All 26 build/test processes passed: Debug/Release on BL12/13/14/15,
the BL14 binary on BL15, release test exclusion and eight prior-save contracts.
There were 580 headless test executions, with zero failures or skips.

`RescueMissionProbe` registers the real campaign behavior and dispatches Bannerlord
events through its activity/uninstall guards. It checks manual and simulated wins
on both sides, prisoners removed before battle end, joining an existing battle,
defeats, unrelated battles and stale snapshots. The real Harmony prison-break
postfix is installed and invoked through `SkillLevelingManager.OnPrisonBreakEnd`:
successful allied-lord rescues grant exactly one completion and 40 doctrine progress;
failed attempts, companions, enemy/clanless lords, ransoms, random escapes, inactive
doctrines and prepared uninstall saves grant nothing.

Campaign/player singletons and native skill/release side effects are controlled
headless fixtures. This does not execute a prison mission or campaign battle in
the running game. No save schema changed; the new battle snapshot is transient.
Live acceptance, including completion persistence, is in `LIVE_GAME_MATRIX.md`.

## Generated module appearance (V2) — 2026-10-20

Evidence: `out/npc-age-20261020/verification-summary.json`. The released exporter
reproduction in `result.log` confirms that a runtime `MBBodyProperty_1` reference
loads as a zero-age range in a fresh object registry.

`NpcModuleExportProbe` exercises the full NPC exporter, discards the source registry
and loads its XML through the real `BasicCharacterObject.Deserialize`. It checks
existing/new IDs, absence of a culture, distinct minimum/maximum ages and face keys,
weight/build, female troops, hair/beard/tattoo tags, and source immutability.
Custom race lookup is a controlled FaceGen fixture; body/face deserialization is
unmodified engine code. Further cases cover the stored appearance behind Body Mod
Compatibility, French decimal separators and failure when body properties are missing.

Debug/Release builds pass against BL12/13/14/15, with no compiler warnings/errors.
The final debug runs also cover the BL14 binary on BL15: 575 test executions,
zero failures/skips, plus eight prior-save contracts and release test exclusion.
The first BL12 test run exposed API differences in the new host fixture; after
correcting its getter/setter access, all five debug profiles were rerun successfully
(`verified-*-on-*.xml`). The production binaries did not need changes for that.

No save schema or campaign data is changed. Previously generated modules need
regeneration from their original Retinues export. Native visual rendering,
the Library dialogs and disk save/reload remain on the live checklist.

## Improved Garrisons recruitment compatibility (V2) — 2026-10-17

Evidence: `out/improved-garrisons-20261017/`. The optional headless runner argument
`--improved-garrisons=<path-to-ImprovedGarrisons.dll>` runs the downloaded mod's actual
volunteer queries and regional transfer method, and Bannerlord's mobile-recruitment
entry point with Retinues' existing AI restriction patch installed. Campaign objects,
available roots, roster writes and cost accounting are controlled fixtures; this does
not launch the game or reproduce the reporter's complete load order/save.

The probe passes against BL14 and BL15. It checks clan-versus-kingdom template selection,
late matching slots, custom elites, exact tiers, unavailable trees, relationship locks,
recruiter/culture restrictions, uninstall gating, non-mutating queries, selected troop
and cost, single-slot consumption, duplicate prevention, exception rollback, nested
scope cleanup, and ordinary AI isolation. Three shared regression cases also cover
restriction combinations, stock preservation, and cyclic/missing upgrade paths.

The downloaded archive is labelled 4.3.0.3 but its manifest reports 4.2.0.7; it is not
proof of an identical build to the reporter's installation. Tested DLL SHA-256:
`C1845966BE591FCF2C35D07BF93348ABB6F50F8D5FF1B512E7FDC0FF8757B122`.
No third-party binary is bundled or required. Missing IG is a no-op; signatures are
validated before patching and partial installation is rolled back on failure.

The standard V2 matrix runs via `Run-Validation.ps1 -V2Only -NoRestore -Repeat 1`, with
the installed Workshop Harmony directory and output `out/improved-garrisons-20261017/validation`.
It covers Debug/Release on BL12/13/14/15, BL14 on BL15, prior-save contracts and release
test exclusion. All 26 processes passed, including eight prior-save contracts, with no
compiler warnings or errors. This integration adds no persistent save fields. Live recruiter travel,
garrison delivery and training remain on the manual checklist.

## Independent tier 8–10 skill caps, both branches — 2026-10-17

Evidence is under `out/tier-caps-20261017/`: V2 uses `Run-Validation.ps1`
with `-V2Only -NoRestore -Repeat 1`; `validate_stable.py` builds an isolated
copy of stable with its reference dependencies. Debug/Release cover BL12/13/14
on stable and BL12/13/14/15 on V2, plus both BL14 Debug binaries against BL15.
The existing prior-save contract checks and release test exclusion checks are included.

Five new regression cases cover independent high-tier cap selection, legacy settings
inheriting the former T7 cap, explicit new settings surviving later loads, stable's
Freeform preset, and V2 settings visibility as the maximum tier changes. Tests restore
all option state and suppress configuration persistence; no user settings are modified.
Native in-game UI confirmation remains manual: raise Maximum Troop Tier to 8, 9, then
10; assign different caps to each tier; check the editor, restart, and check again.
V2 should reveal each new slider at the corresponding tier; stable lists them in MCM.

## Cheat-inventory discard unlocks, both branches — 2026-10-14

The final combined report is `out/validation/discard-unlocks-20261014-complete.json`:
42 successful build/test processes, 579 case executions, zero failures/skips, and
11 prior-save contract checks. Debug/Release builds cover BL12/13/14 on stable and
BL12/13/14/15 on V2. Both BL14 Debug binaries also pass against the BL15 assemblies.
Final builds have no compiler warnings or errors. V2 evidence is under
`discard-unlocks-20261014-verified/`; corrected stable evidence is under
`discard-unlocks-20261014-stable/`. The combined report replaces earlier failed
stable build attempts with their successful reruns.

Four regression cases per branch cover unchanged cheat catalogues, positive additions,
taking/returning items, cancellation, item modifiers, new items, repeated filtering,
independent inventories and unchanged ordinary discards/loot. The isolated host installs
the actual Harmony initializer patch and exercises the production discard handler,
including enabling unlocks and disabling cheats between opening and closing. It also
checks loot/trade/stash/warehouse exclusion. Native campaign initialization is stubbed;
this is not a live inventory UI test. On BL12, the engine already suppresses cheat
discard events, so the probe checks patch installation and the shared filter tests run.

Validation uses `Run-Validation.ps1 -NoRestore -Repeat 1`, the installed Workshop Harmony,
and `-GameDirectory` pointing at an isolated dependency layout copied from stable's
reference DLLs (its default dependency paths point to absent local module folders).
The stable-only rerun script is `out/discard-unlocks-20261014/Run-StableValidation.ps1`.
No deployment was performed. Snapshots are temporary weakly referenced roster state;
no save fields or earned unlocks change. In-game confirmation remains pending below.

## V2 malformed weapon holsters and stale item definitions — 2026-10-12

Validated with `tests/Run-Validation.ps1 -V2Only -NoRestore -Repeat 1`, using the installed
Workshop Harmony directory. Reports: `out/validation/holster-safety-20261012-final/`.
All 26 processes passed: Debug/Release on BL12/13/14/15, the BL14 binary on BL15,
eight prior-save contracts, and release test exclusion. All 410 case executions passed
with no skips; all builds completed without compiler warnings or errors.

Six new regression cases cover invalid holsters in editor/AI selection, cached candidate
revalidation, null and oversized arrays, mixed valid/invalid entries, normal engine
passthrough, and preservation of saved equipment and queued progress. An isolated engine
probe installs the actual Harmony prefix and calls the game's holster lookup. It also
replaces a registered item under the same ID, verifies existing wrappers resolve the new
definition without losing dirty stock/unlock attributes, and resumes a paused upgrade.

The fallback changes only the returned holster indices; shared item definitions and saved
loadouts remain intact. This fix changes no save fields or serialized layouts. The reported
Derthert battle and its full mod load order remain pending live testing; see
`LIVE_GAME_MATRIX.md`. Headless checks establish the specific null-holster protection,
not successful completion of that battle.

## V2 local bug report export — 2026-10-11

Validated with `tests/Run-Validation.ps1 -V2Only -NoRestore -Repeat 1`, using the installed
Workshop Harmony directory. Reports: `out/validation/bug-reports-20261011-final/`.
All 26 processes passed: Debug/Release on BL12/13/14/15, the BL14 binary on BL15,
eight prior-save contracts, and release test exclusion. All 375 case executions passed
with no skips, including 25 executions of five new archive regression cases.

Archive tests verify byte-identical save copies and untouched originals, repeated exports,
missing/locked/oversize files, valid relative entry names, duplicate rejection, bounded
log snapshots, and active-slot-only save selection. Filesystem fixtures use a writable
validation directory because the execution sandbox prohibits renames in its temp folder.
Generated Settings prefabs parse successfully for BL12/13/14 and contain the report button.

The final UI notification adjustment (save/busy rejection inside a closing text inquiry)
is additionally checked by a BL14 build. Native screenshot capture, folder opening, and
the Settings/console workflow remain pending in-game checks in `LIVE_GAME_MATRIX.md`.

## V2 daily-training crash — 2026-10-10

The supplied save and log matched empty, unregistered duplicate troop rows in all 16
reported garrisons. Cleanup now removes these rows on load, before save and during
`retinues.scrub_save`, provided an initialized registered replacement exists. Populated
ordinary troop variants, heroes and rows retaining wounds/XP are preserved. Successful
removals are logged per roster with a total; failed plans report no removals.

Validated with `tests/Run-Validation.ps1 -V2Only -NoRestore -Repeat 3`, using the installed
Workshop Harmony directory. Reports and binaries: `out/validation/daily-xp-fix-final/`.
All 26 processes passed across BL12/13/14/15, including the BL14 binary on BL15. The
engine regression reproduces the null-reference crash before installing the XP patch,
then checks empty-stack training without removing rows during iteration or changing
neighboring populated stacks. Eight prior-save contract checks passed; no save fields
or serialized layouts changed. The reporter's full campaign/load order has not been
tested in-game; its load/train/save/restart scenario is in `LIVE_GAME_MATRIX.md`.

## V2 merged-PR follow-up — 2026-10-07

Validated v2.0.0.14 after merging PRs #25, #26, #27, #29 and #30. The follow-up adds
missing-leader/culture checks for Cultural Triumph and a Bannerlord 1.2 adapter for the
party-added event introduced in later game versions. Regression tests cover missing and
detached leaders, same/different/unknown cultures, the legacy Harmony binding, event
dispatch, and listener reset between campaigns. The AI equipment fixtures now also supply
the tier and random inputs that Bannerlord 1.2 normally obtains from a running game.

```powershell
./tests/Run-Validation.ps1 -V2Only -NoRestore -Repeat 3 -OutputDirectory out/validation/v2-2.0.0.14-verified-20261007
```

- All 26 build/test processes passed: Debug and Release against BL12, BL13, BL14 and
  beta references, plus the BL14 Debug binary running against beta assemblies.
- 780 headless case executions passed, with zero failures or skips, plus 15 supplemental
  engine integration checks. Repetitions/version combinations are not distinct features.
- Eight prior-save contract checks passed against stable and V2 baselines. No save
  fields, type IDs or serialized layouts were changed by this follow-up.
- Release binaries exclude tests and fixtures. Live battle/feat acceptance remains pending;
  these checks do not replace in-game testing of the merged changes.

## Known live-validation issues — 2026-09-18

Moved here from the player-facing changelog; these findings remain unresolved.

- The first live campaign suite stopped after two passes and one cleanup failure, skipping
  the remaining 78 cases: a temporary test troop remained active. The test cleanup fix and
  a complete campaign run are pending.
- On Bannerlord 1.5.3, the full installed mod load order crashed before the main menu. A
  reduced load order with Retinues, its required dependencies and official modules loaded
  a disposable copy of a V2 save from game 1.4.5. The crash cause has not been isolated, and
  save/resave/restart and mission acceptance remain unverified.

## Headless validation

Latest follow-up: the [installed Bannerlord 1.5.4 audit, 6 October](GAME_UPDATE_COMPATIBILITY_2026-10-06.md)
found and fixed a binary-incompatible tooltip constructor in both branches. All 38 processes
against the installed assemblies passed (497 headless executions and eight prior-save
comparisons); the supported-version matrix also passed all 41 processes (807 headless
executions and 11 prior-save comparisons). These are headless checks, not live-game acceptance.

The [second save-preservation pass, 18 September](SAVE_COMPATIBILITY.md#second-preservation-pass--2026-09-18)
records 41 passing processes, 753 repeated headless case executions and 18 supplemental engine
checks after the roster/equipment preservation fixes. The earlier
[community investigation](COMMUNITY_REPORT_INVESTIGATION_2026-09-18.md#validation) and historical
first-run evidence below are retained.

This records the first expanded-suite run. The subsequent prior-version save audit, additional
ammunition-preservation fix and updated 496-case/11-compatibility-probe results are documented
in [SAVE_COMPATIBILITY.md](SAVE_COMPATIBILITY.md).

Tested the current unstaged working trees of stable and V2. No commits or deployments were
made. Reports and exact tested binaries are in `out/validation/verified-matrix/` locally;
this ignored output directory is not distributed with the source.

```powershell
./tests/Run-Validation.ps1 -NoRestore -Seed 12345 -Repeat 3 -OutputDirectory out/validation/verified-matrix
```

## Executed

- 14 mod rebuilds: Debug/Release for stable BL12/13/14 and V2 BL12/13/14/BL14-with-1.5-references.
  All succeeded with zero compiler warnings and errors.
- Seven Release runtime checks verified exclusion of the test runner and embedded fixtures.
- Eight Debug runtime combinations, including a V2 BL14 binary loaded against 1.5 assemblies.
- 481 passing test executions: 111 stable and 370 V2. These are repetitions/version combinations,
  **not 481 distinct features**. There were zero failed or skipped cases in these headless runs.
- All 30 build/runtime processes passed, including the runner build; none timed out.
- Negative CLI checks: `--repeat=0` and using a Debug binary with `--release-check` both returned
  exit code 1 and emitted a failing JUnit report.
- `git diff --check` passed in both checkouts.

| Runtime | Discovered headless cases × repetitions | Supplemental engine cases | Passed executions |
| --- | ---: | ---: | ---: |
| Stable 1.2 | 12 × 3 | 1 | 37 |
| Stable 1.3 | 12 × 3 | 1 | 37 |
| Stable 1.4 | 12 × 3 | 1 | 37 |
| V2 1.2 | 24 × 3 | 2 | 74 |
| V2 1.3 | 24 × 3 | 2 | 74 |
| V2 1.4 | 24 × 3 | 2 | 74 |
| V2 1.4 binary / 1.5 engine | 24 × 3 | 2 | 74 |
| V2 built against 1.5 | 24 × 3 | 2 | 74 |

Inventory at BL14: stable has 86 cases (12 headless, 74 campaign); V2 has 79 cases
(24 headless, 55 campaign). Supplemental host checks are separate from that inventory.

## Confirmed regressions caught during this work

Stable's assertion class carried `SafeClass`, allowing the in-game exception safety patcher
to swallow assertion failures. The attribute was removed; self-tests intentionally fail
assertions and check that they throw, and exercise failure/skip/empty/cleanup result handling.

The frozen save-field contract failed for V2's missing `FactionSaveData.Extras` field 17
(`out/validation/contract-before-fix.xml`). Stable writes that field for edited extra trees.
V2 now reads it and traverses extra roots, descendants and captains during migration.
The field contract and traversal regression pass across all tested V2 engine combinations.

## Not executed

Bannerlord was not launched. The 74 stable and 55 V2 campaign cases were compiled and
inventoried, but were **not run**. This includes the new V2 clone/edit/restore/remove, economy
sharing and staged-queue scenarios. Their actual campaign behavior remains to be verified.

No full `.sav` load/save/restart cycle, UI flow, mission, DLC/load-order combination or
seven-day campaign soak was performed. The timeout mechanism was used as a bound but no
intentional hung-process experiment was run. Follow [LIVE_GAME_MATRIX.md](LIVE_GAME_MATRIX.md)
and retain the resulting evidence before treating a release as game-validated.
