# Prior-version save compatibility audit

## Release ZIP preservation audit — 2026-11-02

Reviewed the actual three test packages prepared on November 1: stable BL1.2
`v1.2.14.34`, stable BL1.4/1.5 `v1.4.14.34`, and V2 `v2.0.0.15`.
The original published `.34` and `.15` archives were retained before those test
packages replaced them. The October 16 source snapshots match the DLL hashes in
those original archives, so the source review covers changes since the shipped
versions, rather than treating all uncommitted work as unreleased.

### Regression found and fixed

The shared stable-character converter linked a captain back to its base troop
before finishing the remaining definitions. V2 redirects several captain
properties to that base. As a result, converting the captain could overwrite the
base troop's skill baseline; an already-linked import could also overwrite its
formation and original source identity. Migrating a separate captain XP entry
could overwrite the base troop's converted XP pool.

Conversion now detaches the destination's back-link, restores each definition and
its progression independently, and links captains in a second pass. Both the save
migration coordinator and stable XML importer use this order. The importer restores
the links in a `finally` block if a record fails. This changes no saved field, type
ID, key, model format, stub ID or ordinary V2 save-loading behavior.

The regression reproduced a saved baseline of **111 becoming 999** before the
fix. New tests cover parent-first/captain-first order, fresh/existing links, the
actual save coordinator with distinct XP entries, the actual stable importer,
and serialize/reload with shared captain XP. The live-campaign import case also
checks formation, source identity and preservation of existing unspent points.

### Evidence

- **40 forward compatibility comparisons** against actual older packaged DLLs:
  18 for the stable builds and 22 for V2. Baselines cover stable releases `.30`
  through `.34`, the BL1.2 `.34` build, and V2 `.11` through `.15`. All pass.
  These check real SaveSystem registrations, field IDs/types and containers;
  prior V2 model payloads also pass current-reader loads and four resaves across
  three cultures, including pending shared XP. Cross-game comparisons here
  concern Retinues data contracts, not native Bannerlord save downgrades.
- **17 same-branch package comparisons** preserve every prior top-level XML
  object definition. The stable packages retain all 1,200 troop definitions,
  including the 200 classic placeholders; V2 retains its 1,000 troop stubs.
- The initial final matrix passed all **41 processes / 2,251 test executions**
  across Debug/Release BL12/13/14/15 where supported. After adding the direct
  importer regression, the focused final pass passed **38 processes / 1,932 test
  executions**, with different randomized seeds, including both BL14 builds
  running on BL15. Counts include repetitions; none failed or skipped.
- Actual ATC 1.12.0 and Improved Garrisons DLL integration probes pass on BL14
  and BL15. Reviewed recruitment hooks do not add save partitions or redefine
  existing troops. Offer discovery leaves world stock intact; temporary transfer
  substitutions restore unconsumed slots. Old T7+ cap settings seed missing T8–T10
  settings without overwriting explicit values. Rescue Mission retains its feat ID.
- The corrected V2 Release ZIP passes patch binding and game-API reference checks
  on BL14 and BL15, excludes test code, and passes all 22 older-release comparisons.
  Its only changed archive entry relative to the November 1 ZIP is `Retinues.dll`.
  It replaced the V2 ZIP in `Modules`; the stable ZIPs were not changed by this fix.

Evidence is under `out/save-audit-20261102/`: `inventory.json`,
`contract-results.json`, `package-preservation.json`, `final-matrix/summary.json`,
`post-fix/summary.json`, and `updated-package.json`. The corrected package and its
file hashes are in `out/releases-20261102/`. V2 ZIP SHA-256:
`be0910d84d04aba55fd364ead93b843ab0af231636cae6cfa8c2e4a4b7c5965b`.

### Limits and older migration boundary

This is a managed-code and package audit, not a complete native `.sav` load in the
game. It cannot prove compatibility with every other mod or an already-damaged
campaign. Existing heavily modded saves still need a real load, play, save and
reload before release. No player save was edited during this audit.

V2 already omitted the 200 **classic `ret_` troop definitions** carried by stable
for very old saves; this patch does not introduce that omission. A save that still
uses those old IDs needs the stable migration bridge before moving to V2. Stable's
`TroopSaveData.Deserialize` reallocates these IDs and transfers roster references,
XP and queued changes. Passing the legacy type-contract comparison alone does
not prove that a direct jump from those ancient saves into V2 is safe. Neither
V2-to-stable downgrade nor a Bannerlord-version downgrade is certified here.

## Second preservation pass — 2026-09-18

Reviewed the current changes in both checkouts against stable `ff69d116` and V2 `96a9dc90`,
including load/save hooks, migration, saved identifiers, roster repairs, equipment cleanup,
queued work, shared XP, doctrine state and the new behaviors. This pass found and fixed two
additional preservation risks:

- **Both branches: duplicate roster repair.** Removing a row and re-adding it runs engine
  campaign/XP callbacks. Those callbacks can clamp banked XP or throw after modifying the
  old row. Repair now prepares a copy, replaces only non-hero custom-troop references, merges
  duplicate counts/wounded/XP with overflow checks, then installs the completed result and
  invalidates the row cache. It does not invoke recruitment or XP-clamping callbacks. A
  resolver failure or overflowing merge leaves the original rows untouched.
- **V2: automatic equipment cleanup.** Editor visibility was being used as permission to
  delete existing gear and unresolved queued IDs. Cleanup now removes identified unsafe
  siege/mission equipment rather than everything hidden by the editor. Unresolved queued
  IDs preserve their order and progress, survive resaves and wait without accruing more work.
  A permanently unavailable item therefore blocks that queue until restored or replaced by
  the player, instead of silently discarding the saved order.

Four new roster cases run on both branches: counts/wounded/XP and cache preservation;
generated duplicate stacks in varied orders; resolver failure after a planned replacement;
and overflowing XP merges. Three V2 cases exercise real managed equipment models: hidden
saved gear, unresolved queued work through three model resaves, and removal of unsafe gear
without losing a valid replacement or its progress. The existing campaign equipment test
also now expects unresolved IDs to survive.

The pre-fix equipment run reproduced all three preservation failures
(`out/save-compat/recheck-20260918/equipment-before.xml`). Engine source inspection confirmed
the destructive roster callback path; the initial isolated roster run also failed because
the old repair required campaign callbacks. These are managed tests, not full `.sav` loads.
The BL12 fixture was corrected to seed loaded rows directly: even constructing a dummy row
through BL12's ordinary add API invokes XP clamping.

The registration probe now discovers **every module save definer**, including V2's behavior
registry (currently no additional class/container contributions). The old registered fields,
IDs, container types, model payloads and shared-XP payloads still pass all eleven comparisons.
This pass adds no saved field, key, format version or persisted identifier.

Validation uses `tests/Run-Validation.ps1 -NoRestore -Seed 20260918 -Repeat 3` with reports in
`out/save-compat/recheck-20260918/verified/`:

- **41/41 processes passed:** 14 mod builds, seven Release-exclusion checks, eight Debug
  runtime combinations, eleven prior-version comparisons and the runner build.
- **771 passing test executions:** 753 discovered cases across repetitions/engine versions
  (stable 17 × 3 × 3, V2 40 × 3 × 5) plus 18 supplemental engine checks. No failures or skips.
  These are repeated executions, not 771 distinct features.
- Both changelogs are updated. Builds disable deployment; no installed mod or user save is
  changed by this audit.

This supports forward compatibility with the two named revisions. It cannot prove every
historical save or external mod combination safe. Full campaign load/save/restart/reload,
native object reconstruction and event ordering remain live acceptance steps. Downgrading
a newly saved campaign to the old V2 implementation, which predates normalized XP, remains
unverified.

## Earlier audit — 2026-09-09

Follow-up on 18 September: the [community investigation](COMMUNITY_REPORT_INVESTIGATION_2026-09-18.md)
also removed the separate blanket rejection of ordinary throwing stones. Saved-ammunition
regressions and all eleven prior-version contract comparisons pass in the new matrix. That
follow-up introduces no saved fields or keys; full campaign save acceptance remains unverified.

The baseline is each checkout's last committed revision before the current unstaged changes:
stable `ff69d116` (patch 31) and V2 `96a9dc90` (patch 12). This identifies the precise code tested;
it does not assume a Nexus download or Steam package contains that same revision.

Both revisions were exported with `git archive` into ignored `out/save-compat/` directories
and rebuilt in Release against the local BL14 references, with deployment disabled. The old
working trees were not checked out over the current changes. Both prior builds succeeded
with zero warnings/errors.

## Checks and findings

The new headless probe calls the actual prior/current `SaveableTypeDefiner` registration
methods using separate engine `DefinitionContext` instances. It records actual numeric save
IDs, every registered field ID/type/name and constructed container signature/ID. This covers
troops, bodies, equipment, skills, factions, combat statistics, training/equipment queues,
equipment policies and the older legacy troop type.

- Stable -> stable: all **10 classes, 97 fields and 22 containers** remain compatible.
- Prior V2 -> current V2: all **10 classes, 96 prior fields and 22 containers** remain compatible.
  V2 adds only the existing stable faction `Extras` field 17. Missing old values stay null.
- Stable -> V2: all **10 classes, 97 fields and 22 containers** match the legacy reader.
- Stable save definitions and the code writing its existing save partitions are unchanged.
  The changed feat retains its persisted class/key (`PS_AllyArmyWin3`) despite its lower target.
- V2's model format/version, compression/chunk keys, wrapper identifiers and existing attribute
  names are unchanged. The new Debug-only model fixture does not derive from `WBase`, so the
  wrapper persistence scanner excludes it. Release test/fixture exclusion also passes.
- V2's new shared-progress key is additive. A prior binary wrote seven shared points and 33,000
  raw XP through its real `SyncData`; the current reader retained both, and preserved them on
  resave when no eligible troop was available for conversion. Existing normalized-progress
  continuation and one-time conversion regression checks also pass.
- A generic model payload written and compressed by the prior V2 binary survived the current
  reader and four successive resaves under en-US, fr-FR and tr-TR. It includes Unicode/XML
  characters, integers, float/double values, flags, stock maps and a staged queue including an
  empty-item unequip entry. This tests actual model serialization, not campaign item resolution.

The exported prior contracts/payload are frozen in:

- `HeadlessAudit/Fixtures/stable-ff69d116.xml`
- `HeadlessAudit/Fixtures/v2-96a9dc90.xml`

The normal matrix now reruns eleven prior-version compatibility comparisons against Release
binaries: three stable -> stable, four prior V2 -> V2 and four stable -> V2 across the relevant
engine references. A removed/renumbered field, reused class ID, altered container or lost model
value fails the process. Empty baseline files are rejected.

## Data-loss risk fixed during this audit

The new V2 load-time scrub used `NotMerchandise` alone to reject ammunition. This could remove
legitimate non-sale arrows/bolts supplied by other mods from an existing troop or staged queue.
The installed game data also contains non-sale `arrow_emp_1_a` and `blunt_arrows`; that flag
alone does not establish that gear is invalid.

The predicate now identifies the known ballista, burning-bolt and tournament-ammo IDs before
using that flag to reject ammunition. Other non-sale ammunition is preserved. Existing checks
for siege projectile weapon classes remain in place. A managed real-ItemObject regression
failed before this change and passes afterwards for custom/vanilla non-sale ammo while still
rejecting the known mission ammunition (`out/save-compat/ammo-before-fix.xml`).

## Evidence and limits

`out/validation/save-compat-verified/` contains the current build, headless and compatibility
results. The compatibility checks are added to `tests/Run-Validation.ps1` so they remain part
of normal validation. Earlier run evidence is retained separately in `VALIDATION.md`.

All 41 matrix processes passed: 14 clean mod builds, seven Release exclusion checks, eight
Debug runs totaling 496 passing executions, eleven compatibility probes and the runner build.
There were no failed/skipped headless cases. Both checkouts passed `git diff --check`.
Additional negative probes rejected a deliberately renumbered old field and an empty baseline
with exit code 1; the unchanged old fixture continued to pass with the same final runner.

These checks support upgrading old saves. They do not establish downgrade compatibility for
saves written with the new normalized-XP key. The upgrade intentionally converts legacy shared
XP and repairs recognized invalid gear/roster references; it is not a byte-for-byte identity
operation on campaign data.

Bannerlord was not launched and no user `.sav` was opened or changed. Complete save-file
deserialization, campaign event ordering, native object reconstruction and mod load-order
interactions still need a real prior save loaded, saved and reloaded after restarting the game.
Follow `LIVE_GAME_MATRIX.md`; the managed fixtures do not certify those runtime steps.
