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
| Malformed weapon holsters (V2) | Load a copy of the reported Derthert battle save with its matching mods; deploy both armies, then save/restart and repeat | No holster null-reference crash; fallback log identifies affected item IDs without flooding; equipped items remain; malformed weapons are excluded from new choices and pending upgrades retain progress until corrected |
| AI/simulation | Auto-resolve, AI retinues, village recruitment, player/other militia | No unintended custom retinue recruitment or AI upgrades; militia spawns in expected lanes |
| Tournament | Exclusion on/off, with and without arena overhaul | Full valid bracket; mission completes; exclusion policy respected |
| Naval | Embark/disembark, naval battle, edit Mariner, reload | Engine/editor agree on trait; naval gear correct; land behavior unaffected |
| Convoy editor, both branches | With War Sails, open each culture in the Global Editor, edit convoy guards/veterans/masters/traders, change one troop's culture, reopen, export/import and save/restart twice. Repeat editor startup without War Sails and on BL 1.2 | Four separate native convoy roles where available; edits survive and remain under their original culture; land caravans unchanged; absent convoy data produces no section or errors |
| Doctrines/unlocks | Kill/victory/reward feats, loot multipliers, crafted-design unlocks | Expected progress once per event; no duplicate rewards; owned IDs remain selectable |
| Rescue Mission (V2) | With Pragmatic Scavengers in progress, rescue a same-kingdom lord by winning a battle manually, auto-resolving, joining an ongoing battle, or completing a prison break. Test each from a disposable pre-completion save; also test a defeat, failed prison break, ransom, unrelated lord and a locked/acquired doctrine | Each eligible rescue completes Rescue Mission once and grants 40 doctrine progress. Defeats, failed rescues, ransoms, unrelated prisoners and inactive doctrines grant nothing; completion survives save/reload |
| Cheat inventory discards, both branches | On a disposable campaign with locked gear, enable unlock-by-discard and cheats; close inventory unchanged, then discard one item, take/return cheat items, undo/cancel transfers, and repeat with modified equipment. Disable cheats before closing one cheat screen; test normal inventory and unclaimed battle loot too | Only positive additions to the opening cheat catalogue earn progress; untouched catalogue items remain locked; modifiers count correctly; ordinary discards and loot retain their previous behavior; `[Unlocks] Cheat inventory filtered` records the filtering; existing unlocks survive save/reload |
| Disk saves | Save/menu/reload, then full exit/restart/reload; save and repeat | IDs, names, skills/body, policies, stocks, XP, queues and doctrine progress preserved |
| Empty obsolete troop entries | Load a copy of the reported daily-training crash save with its matching mods; inspect cleanup logs, advance several days, save/restart and repeat | Per-roster removal logs and a total on first cleanup; populated troops/wounds/XP preserved; no repeated removals after saving unless new stale entries appear; no daily-training XP exceptions |
| Migration | Inspect canonical/extra descendants/captains; save/restart twice | Values/links survive; migration does not reapply; absent old fields default correctly |
| Uninstall, both branches | Mature campaign with custom/legacy troops in player and AI parties, garrisons, militia, settlement prisoners, village volunteers and a troop quest; record counts/wounds/XP, then purge | Verified success; replacement mode preserves counts/wounds/XP; native troops and other mods' troops remain; removal mode only removes custom roster rows; no new XP errors |
| Prepared-save reload, both branches | Save to a new slot, exit, reopen with Retinues still enabled; open recruitment/party screens and advance a day, save again | No recreated custom troops; no editor changes allowed; uninstall marker survives; no Retinues-only payloads written |
| Actual uninstall, both branches | Exit, disable/remove Retinues, load prepared save, recruit at owned and foreign villages/castle villages/towns, advance a day/week; save and restart twice | No missing troops, crashes, recruitment/training exceptions or dependency on Retinues; active troop quests remain usable |
| Failed uninstall | Unsupported/conflicting reference or simulated setter failure on a disposable fixture | No success message; no mutation on preflight failure; saving blocked after an application failure; original backup still loads normally |
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

## Bug report export (V2 2.0.0.15)

- In a loaded campaign, use Settings → Support → Bug Report. Check the dialog fits at the lowest supported UI resolution/scale in English and French. Type multiple paragraphs, paste enough text to scroll, then edit using mouse selection, Shift+arrows, Home/End, Ctrl+A/C/X/V and undo/redo. Check accented characters and the 8,000-character limit. Enter must add a line without submitting. Mouse wheel and scrollbar must reach every line.
- Cancel once with the button and once with Escape: only the report window should close, no ZIP should be created, and the editor should retain focus. Reopen, export a description with blank lines and literal `<item>` text, and confirm the exact text reaches report.txt. Confirm the dialog closes before the screenshot and the output folder opens. Opening the dialog must pause the campaign and closing it must release only its own pause request.
- Inspect the ZIP: last saved campaign, screenshot, logs, settings and ordered mod versions. Compare the source save's hash before/after; export must not save or overwrite it. Confirm collection doesn't stall the UI or prevent a normal save.
- Run `retinues.bug_report test` from another screen, close the console within three seconds, and check the captured screen. Repeat from the main menu and an unsaved campaign: no unrelated save may be attached.
- Retry during an active save (the description must remain available after the error), repeat clicks while collecting, and move between campaigns during the screenshot delay. Check clear feedback and no mixed-campaign report. Check capture on BL12/13/14 and the 1.5 beta; renderer capture and opening the folder require live testing.

## Direct import of stable exports (V2)

- Copy a current stable export into Documents/Retinues/Exports. Check the Library label, troop preview, section selector and final destination confirmation. Cancel each dialog once and confirm no troops change.
- Import clan and kingdom sections separately with basic/elite trees, both retinues and captains. Check names, skills (including a zero skill), formation, appearance, battle/civilian sets, shared upgrade branches and captain bonuses. Save/reload and export to V2; import that V2 file again.
- Import a sparse culture export containing only an edited caravan master: guards and veteran guards must remain unchanged. Repeat for convoy Extras with War Sails loaded; missing destination rosters must be reported as skipped.
- Try an export with an unavailable item/culture mod: import must stop with a dependency error before changing troops. Restore the dependency and retry.
- Run the `import` and `migration` campaign test groups on a disposable save. Headless parser tests do not validate the live editor or native character rendering.

## Improved Garrisons recruitment (V2)

- Use a disposable campaign with distinct clan and kingdom trees. Set a kingdom-only IG training template, allow custom recruitment in the relevant settlements, and set Allowed Recruiters to Everyone. Enable regional recruitment, then send a recruiter to another eligible settlement. Verify both paths hire kingdom troops, consume village stock, deliver to the garrison and train toward the template.
- Repeat with custom elite targets and IG's elite-only recruiter setting. A basic-tier slot must not become an elite/higher-tier troop. Check mixed clan/kingdom templates and a desired troop in a later volunteer slot.
- Check FactionOnly, PlayerOnly, Same Culture Only, disabled clan/kingdom availability, mixed vanilla troops and locked relationship slots. Recruitment must respect those restrictions; player-only custom troops must not be recruited automatically.
- Check ordinary player and AI recruitment, both mod load orders, save/reload, and a session without IG. Confirm the compatibility installation log appears once, no hook errors occur, and consumed slots stay empty. IG's travel rules, recruitment budgets, cheat spawning and training logic remain its responsibility.

## Adonnay's Troop Changer recruitment (both branches)

- With ATC and an overhaul that defines additional volunteer trees, enable custom regular and elite recruitment in an owned settlement. Check both types in the recruit menu, including original recruits also used as militia. Buy one, close/reopen the menu, save/reload and check that the purchased slot stays empty until normal replenishment.
- In stable, revisit a settlement whose old pool already contains custom militia. Only militia with a recorded original that belongs to the current ATC pool should be corrected. Check the settlement's actual militia roster and equipment remain unchanged.
- Check custom troops disabled, locked/missing elite trees, vanilla mixing, culture restrictions, clan/kingdom availability and AI recruiter permissions. Observe an allowed AI lord hiring and check the charged troop and party roster. Repeat without ATC; ordinary recruitment must remain unchanged.
- Keep ATC above Retinues as recommended. Check the compatibility activation log, daily replenishment and save/reload. Headless tests use synthetic ATC configurations; a live DRM campaign is still needed to validate that overhaul's specific pools and interactions.

## Generated troop modules (V2)

- Edit several troops with distinct age/height/weight/build ranges and faces, including a female troop. Export them, then use Library → Create Mod Files. Restart with the generated module enabled and start a fresh campaign without importing the Retinues export. Check the encyclopedia, troop previews and spawned battle agents: they must retain the chosen adult appearance and variation. Save, exit and reload.
- Repeat with a non-default race when its supplying mod is available, and with Body Mod Compatibility enabled. The generated module must preserve the stored source appearance. Regenerate any module created by the affected exporter from the original Retinues export; existing generated XML is not automatically repaired.
