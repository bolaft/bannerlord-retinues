# Unresolved community reports: investigation follow-up

18 September 2026. Follow-up to [the public report inventory](COMMUNITY_REPORT_TRIAGE_2026-09-18.md), using both current working trees, the relevant game assemblies, and the reporter's crash attachment. No game launch, deployment, save overwrite, or public reply was made. Confirmed code defects below are fixed locally; reports requiring the original save are still open.

## Corrections made

| Branch / issue | Finding and change | Verification and limit |
| --- | --- | --- |
| Stable: disabled Cultural Pride, Nexus 1129461 | The event dispatcher included unfinished feats even when their doctrine was disabled. It now excludes `IsDoctrineDisabled` entries. This covers equipment costs off and a zero multiplier without erasing progress. | `DisabledDoctrinesDoNotReceiveFeatEvents` checks actual Cultural Pride definitions, enabled handlers, both disabling conditions, and re-enabling. The test isolates MCM accessors because MCM is absent from the headless host. A battle/tournament notification check remains live. |
| V2: Royal Stewardship, part of Nexus 1133924 | Clan settlements can include villages. Dereferencing `s.Town.Governor` could stop the daily scan before a valid governor. The scan now skips missing settlements/towns/governors and still awards at most one daily increment. | `RoyalStewardshipSkipsSettlementsWithoutGovernors` uses a real managed settlement lacking a Town component. It checks the null path, not a positive 30-day campaign. Royal Levy is separate and remains open. |
| Stable: face resets on opening appearance editor | `HeroAppearanceHelper.PrimeFaceGen` called `DefaultFace`, replacing the loaded appearance. Ported V2's existing `RefreshCharacterEntity` call. | Compiles against all three stable targets. Confirm/cancel and main-hero/companion round trips require the native UI. This does **not** establish a fix for the separate wrong-hero-target report. |
| V2: legitimate throwing stones removed by equipment cleanup | The `WeaponClass.Stone` exclusion rejected ordinary looter weapons as well as loose stones. Removed that blanket exclusion; Boulder and identified mission-ammunition filters remain. | Previously failing `OrdinaryThrowingStonesAreValidSavedGear` now passes alongside the non-sale/mission-ammo regressions on every V2 target. This preserves stones still present; it cannot restore items already removed from an older save. |

Production files: stable `Doctrines/FeatServiceBehavior.cs` and `Game/Helpers/HeroAppearanceHelper.cs`; V2 `Behaviors/Doctrines/Feats/Equipments/RoyalPatronage.RoyalStewardship.cs` and `Domain/Equipments/Wrappers/WItem.cs`, relative to each checkout's `src/Retinues/`. Both changelogs are updated.

## Second investigation pass

- **Confirmed and fixed: wrong player-recruitment event.** The actual `RecruitmentVM.OnDone` implementations inspected in game 1.2, 1.4 and 1.5 emit `OnUnitRecruited`. The engine's event dispatcher keeps that separate from `OnTroopRecruited`, which the AI recruitment behavior emits. V2's Royal Levy and Raise the Vanguard listened only to the latter and then required the main hero as recruiter. Added a distinct player-recruitment hook in `BaseCampaignBehavior` and moved both feats to it. Invalid counts and null troops are ignored; ordinary AI recruiting cannot advance these player feats. Missed past recruitment cannot be reconstructed from the save.
- **Confirmed and fixed: V2 retinue conversions did not advance Raise the Vanguard.** The normal V2 party-screen conversion commits `OnPlayerUpgradedTroops`, not a recruitment event. The feat now counts positive non-retinue-to-retinue conversions through that hook too. Retinue-to-retinue and ordinary troop upgrades are excluded. The native `PartyScreenLogic.FireCampaignRelatedEvents` dispatch was inspected, and the source/target/count rules plus the feat's hook are covered headlessly. Live acceptance should exercise a committed five-unit conversion, a cancelled conversion, and a retinue-to-retinue conversion.
- **Confirmed and fixed: parked repeatable feats.** An old save with a repeatable feat at its target failed `LoadedRepeatableFeatCanResume`: `GetState` returned Completed, disabling the event handler before its existing recovery code could run. Repeatable feats now remain active while their doctrine is in progress; the next eligible event credits the parked completion and preserves new progress. One-time feats, zero-target definitions and acquired doctrines remain gated. This is relevant to stalled Royal Levy and older repeatable-feat complaints; it is not a mission reproduction of Defender of the City.
- **AI safety guard, reporter cause still unconfirmed.** Both equipment mutation methods and AI encyclopedia visibility now reject non-custom troop IDs and heroes, even if a saved retinue list references them. No saved links or player edits are deleted. The regression includes ordinary imperial and overhaul IDs plus an allowed custom ID. This closes that permissive update path but does not prove how the reported imperial-tree changes originated or automatically restore already changed equipment.
- **XP diagnostics, not a freeze cure.** Added a Harmony finalizer on the engine XP-clamp method (version-specific target). On failure it logs party/roster identity, troop ID, counts/XP, upgrade IDs including null entries, and whether the registered troop is the same object. Logs are limited to 16 distinct cases per session and reset with runtime state. It preserves the exception and does no repair or XP conversion. A supplemental test installs the real patch, deliberately invokes the engine with an invalid roster element, verifies diagnostic capture, and verifies that the original null-reference failure still reaches the caller.

New focused coverage checks real engine event dispatch, count preservation, inactive/null/negative event rejection, the two feats' player-event subscriptions, parked saved progress, AI target selection, and formatting broken troop data. The test listener is disabled by default so automatic behavior discovery cannot register it in a campaign. The first matrix exposed test-fixture differences (internal `MbEvent.Invoke` on 1.2 and a reused dummy ID across repetitions); those fixtures were corrected before final verification.

## Priority reports: current status

### V2 daily-training freeze — Nexus 1127726

Read `crashreport4.html` from the reporter's [public crash folder](https://drive.proton.me/urls/C2TV8G1JG4#3RdUjOKs8w3t). It records **Bannerlord 1.4.8, Retinues 2.0.0.12, RoT Core/Content/Dragon 8.1.8 and Map 8.1.8.2**. The null-reference stack runs through `PartyBase.OnXpChanged`, `TroopRoster.AddXpToTroopAtIndex`, and `MobilePartyTrainingBehavior.OnDailyTickParty`.

The attachment lists Retinues' prefix on `OnXpChanged`; `AddXpToTroopAtIndex` has an Adjustable Leveling prefix and a Retinues postfix. Patch presence alone does not identify responsibility. The engine method dereferences the roster character, its upgrade-target array and upgrade costs. The Retinues prefix deliberately returns to vanilla for several categories, including non-faction troops, prisoners and troops with upgrade targets. The report contains no failing troop/party identity or local values to choose between those paths. The roster's `OwnerParty` property exists in the 1.4 engine, so a missing-property theory is unsupported.

**Result:** failure path and actual versions verified; root cause not established. The new `[XP fault]` log entry will capture the offending element if this exception recurs with the patch. Next evidence is the pre-freeze save, same load order, exception report and Retinues debug log. Compare the same daily tick with XP-related integrations isolated. A silent hang that throws no exception will need a thread dump instead. The shared-skill-point patch addresses a different mechanism.

### Stable non-infantry house-guard battle crash — Nexus 1132876

Reviewed stable formation-class overrides, mission equipment assignment and the 1.4 engine's general/bodyguard deployment. The native formation query also derives its class from troop composition; overriding that result is not by itself evidence of this crash. Stable does not use V2's 1.5 bodyguard-hook adapter.

**Result:** no confirmed cause or matching fix. Need the crash stack and otherwise identical infantry/archer/cavalry/horse-archer guard variants, with formation overrides on/off and ordinary custom troops as controls. Preserve the game 1.4.8 + RoT context. Do not close this as fixed by the V2 1.5 patch.

### V2 Royal Levy — Nexus 1133924

The second pass found and corrected the engine event mismatch described above. The player hook still requires a positive recruit count, a player kingdom, and troops belonging to that kingdom. Clan-only troops do not satisfy that final condition. V2's wrapper equality is value-based; stable's previously fixed reference comparison was not the cause here.

**Result:** event-wiring defect fixed locally and repeatable saved progress unblocked. In-game acceptance: with Royal Patronage in progress, recruit actual kingdom troops through the volunteer screen and verify the exact count; then recruit clan-only troops and let an AI lord recruit as negative controls. Complete a 100-recruit cycle and confirm the 15 doctrine points plus the next cycle. Keep the public report open until its original reproduction passes.

### V2 AI changing ordinary cultural troops — Nexus 1125711 and related posts

AI creation allocates a custom stub. `CharacterCloner` replaces the copied equipment-roster reference, invalidates the wrapper cache, detaches skills and copies equipment into new objects. The relevant `_equipmentRoster` field exists in the inspected 1.4 engine. The normal path therefore contains explicit isolation from the source troop.

AI upgrade/loot paths trust stored clan retinue links. `GetRawRetinues` filters invalid/underage entries and duplicate IDs, but does not prove every persisted reference is a legitimate custom retinue. The existing player-retinue cross-link cleanup covers a different case.

**Result:** normal copying reviewed; the newly guarded AI update paths no longer permit ordinary/overhaul troop IDs. Bad saved links or an integration remain possible, and links were not removed. Compare the reported ordinary troop's object ID, equipment-roster identity and upgrade tree before/after a daily AI upgrade, and inspect every clan retinue link that names it. This report remains open pending that evidence.

### External recruiters expose inactive stubs — Nexus 1124993 and Steam replies

Inactive `retinues_custom_*` objects remain registered for persistence. Roster canonicalization repairs duplicate object references; it does not change a third-party troop picker or remove saved external assignments. A Steam reply's stale `AIAssignments.xml` entry is a concrete integration lead, not proof for every child/naked-troop report.

**Result:** no universal fix established. Test Convert/Choose Your Troops and Improved Garrisons separately with their saved assignments, before and after deleting/recreating a troop. Record the exact stub ID and active state. Automatically unregistering these objects would threaten existing saves and was not attempted.

## Other source-level findings

- Stable auto-join has no direct formation-class gate; tier costs, renown reserve, per-retinue limits and party capacity can explain slower replacement. No class-specific fix claimed for Nexus 1126061.
- Castle-village selection uses elite/basic source matching and similarity scoring, not equal sampling across imported branches. Nexus 1122567 needs source/target IDs and availability checks before treating unequal recruitment as a defect.
- V2 intentionally excludes non-main-party training from skill-point awards. Garrison roster XP and Retinues skill points are distinct; this does not explain reported missing main-party training.
- Engineering-for-guns and skirmisher class behavior need overhaul-specific fixtures; V2 already has formation overrides. UI overflow, TOAM colors, co-op persistence, naval convoy composition and import/reversion reports remain unverified. The original inventory retains their individual follow-ups.
- The live campaign test cleanup failure is still outstanding: `WCharacter.Remove` rejects roots, including an isolated temporary clone. These headless results do not certify the campaign suite or resolve that pre-existing test-harness problem.

## Validation

Executed:

```powershell
./tests/Run-Validation.ps1 -NoRestore -Seed 20260918 -Repeat 3 -OutputDirectory out/community-investigation/continuation-final
```

- **41/41 processes passed**: runner build, 14 Debug/Release mod builds, eight Debug runtime combinations, seven Release checks and eleven prior-version save-contract comparisons.
- Stable 1.2/1.3/1.4: 13 headless cases repeated three times per target. V2 1.2/1.3/1.4/1.5 plus the 1.4 binary against 1.5: 33 cases repeated three times per combination.
- **612 discovered-case executions plus 18 supplemental engine checks passed**, with zero failed or skipped discovered cases. These are repetitions and runtime combinations, not 630 distinct features.
- Prior stable-to-stable, V2-to-V2 and stable-to-V2 save contracts pass. This follow-up changes no save IDs, fields, model keys or feat keys.
- Exact binaries, JUnit reports and process logs are retained in the ignored output directory above. No full campaign save/load/restart, native UI or mission reproduction was performed.

Keep the crash and compatibility reports open until their original reproductions pass. The fixes above are concrete local corrections, not claims that every report with similar symptoms is resolved.
