# Validation evidence — 2026-09-09

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
