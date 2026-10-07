# Live-game acceptance matrix

These scenarios are separate from headless results. None is complete until someone runs the
game and records evidence. Keep named, disposable starting saves; archive the originals.

## Fixture profiles

| Profile | Required state | Purpose |
| --- | --- | --- |
| Fresh stable / fresh V2 | Standard Native/Sandbox dependencies, no naval DLC | Initialization and vanilla fallback |
| Mature stable -> V2 | Custom trees/captains, edited vanilla/extra trees, stock, unlocks, doctrines, XP, queued work | Complete, one-time migration |
| Mature V2 -> current V2 | Retinues, mixed equipment policies, crafted gear, partial XP and queues | Upgrade persistence |
| Naval DLC | Naval troops, equipment, Mariner skill and naval-ready party | Skill and naval mission behavior |
| Supported total conversion | Real load order with changed cultures, extra trees or custom skills | Compatibility and missing-template assumptions |
| Arena overhaul | Load order changing tournament participants | Bracket capacity and mission startup |

Record exact game/module/dependency versions and load order. Run applicable profiles on each
version intended for release. Cached 1.5 DLL checks do not replace a 1.5 game run. Test default
settings, then disable the feature toggles relevant to the change and check vanilla fallback.

## Scenarios

| Scenario | Exercise | Acceptance observations |
| --- | --- | --- |
| Baseline | Load, enter/exit town, encounter a party, open/close editor pages | No new Retinues errors/crashes; vanilla menus and troops usable |
| Creation/editing | Create basic/elite retinues, clone/import/export trees, edit skills/body/equipment, remove child/captain | Correct identity/ownership; source unchanged; no cross-links; army and encyclopedia agree |
| Economy | Empty/partial stock, repeated copies within a set, shared copies across sets, preview, editor modes, insufficient gold | Exact stock/gold deltas; no preview charge, negative balance or duplicate refund |
| Staged work | Queue slots, replace/cancel head/tail, travel/rest with travel settings on/off, save halfway | Correct order/timing; invalid work stays removed; completed gear is real equipment |
| Party transactions | Single/five/all upgrades at cap, wounds, transfers, cancellation/reopen | Pending counts respected; exact troop/XP/gold deltas; cancel restores state; vanilla upgrades work |
| Player battle | Field, siege assault/defense; victory, defeat, retreat | Correct equipment/formation/captains; no invalid/naked troops; casualty/reward/XP/feat totals consistent |
| AI/simulation | Auto-resolve, AI retinues, village recruitment, player/other militia | No unintended custom retinue recruitment or AI upgrades; militia spawns in expected lanes |
| Tournament | Exclusion on/off, with and without arena overhaul | Full valid bracket; mission completes; exclusion policy respected |
| Naval | Embark/disembark, naval battle, edit Mariner, reload | Engine/editor agree on trait; naval gear correct; land behavior unaffected |
| Doctrines/unlocks | Kill/victory/reward feats, loot multipliers, crafted-design unlocks | Expected progress once per event; no duplicate rewards; owned IDs remain selectable |
| Disk saves | Save/menu/reload, then full exit/restart/reload; save and repeat | IDs, names, skills/body, policies, stocks, XP, queues and doctrine progress preserved |
| Migration | Inspect canonical/extra descendants/captains; save/restart twice | Values/links survive; migration does not reapply; absent old fields default correctly |
| Soak | One hour/day/seven days; alternate travel, towns, battles, editor; save/restart checkpoints | No growing errors, active-stub leaks, duplicate rewards, progressive tick slowdown or failed saves |

Write down expected costs/counts before transactions. Compare concrete values after mission
and save transitions, not just absence of crashes. Record load/save/tick time and memory on
the same machine/fixture against the prior release; investigate sustained growth.

## Run record

```text
Date / tester:
Branch / commit / working-tree snapshot / DLL SHA-256:
Game / DLC / dependencies / load order:
Fixture ID / archived original / disposable copy:
Configuration differences:
Headless matrix report:
Campaign report / seed / repeat:
Scenario / expected values:
Observed before and after:
Outcome: PASS / FAIL / SKIPPED / NOT RUN
Logs / screenshots / resulting save identifiers:
```

Release acceptance requires evidence for the relevant profiles, resolved failures and
accounted-for skips. A successful build, headless case or model serialization call does not
complete a mission or disk-save scenario.
