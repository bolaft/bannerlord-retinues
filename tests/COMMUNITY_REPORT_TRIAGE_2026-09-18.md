# Community report triage — 18 September 2026

Compared public reports with the current working trees of **main** (`ff69d116` plus local changes) and **unstable / V2** (`96a9dc90` plus local changes). This is a source-and-code review, not confirmation that the reporters' saves now work. The follow-up investigation below includes headless regressions and cross-version builds; no live game tests were run and nothing was posted or changed on either site.

**Follow-up:** [Investigation findings and validation](COMMUNITY_REPORT_INVESTIGATION_2026-09-18.md) records the crash-attachment analysis, local corrections, and remaining reproduction requirements. Its second pass fixes the player-recruitment event used by Royal Levy/Raise the Vanguard, unblocks old repeatable-feat progress, protects ordinary troops from AI retinue updates, and adds XP-failure diagnostics. The priority sections below retain the original triage leads except where updated with a confirmed local change.

## Sources and scope

- [Steam Workshop description and FAQ][workshop], including the published compatibility advice.
- [Steam comments][steam]: the latest 50 comments, covering 7 August–15 September, including replies.
- [Nexus posts][posts]: pages 1–9 as displayed on the review date, covering the recent discussions through the 12 August release and several older threads with newer replies.
- [Nexus bugs][bugs]: surveyed the 25 New, 8 Being looked at, and 17 Need more info rows. Some of the latter are explicitly closed. Read the bodies/replies of the first 21 New reports. Four older New report bodies and attempted older-status expansions remained at “Loading issue”; these are explicitly left unassessed below. The Known issues filter was empty.

This does not cover the entire historical comment archive, closed bug archive, or every separate Steam discussion. Dates below are 2026 unless stated otherwise. Nexus bug IDs identify the rows on the linked tracker; report metadata sometimes names the stable download even when the title/body explicitly says V2.

**Status meanings:** “Local match” means the unpushed implementation addresses the described mechanism, but the original reproduction still needs testing. “Candidate” means a related change exists but the cause is unconfirmed. “Open” means no matching fix was found. “Previously released” means it should not be credited to the unpushed patch.

## Reports matching the unpushed patch

All paths in the code column are relative to the named checkout's `src/Retinues/` directory.

| Report / source | Branch and assessment | Local code evidence / remaining check |
| --- | --- | --- |
| Nexus **1118477**, Mariner missing outside the editor; EthienVonStahl and others, 15 Aug–1 Sep. Also Steam SteelBlood, 10 Sep. [Reports][bugs] | **Stable — local match.** | `Game/Wrappers/Character.cs:1414` now writes the engine Mariner flag through the available setter/backing field. Check an existing Nord-derived troop in party, encyclopedia and a naval mission after reload. |
| Nexus **1122164**, default gear in battle, and **1119917**, naked troops. Also multiple comments. [Reports][bugs] | **Stable — strong candidate.** | `Game/Helpers/CharacterObjectHelper.cs:169` and `Game/Wrappers/Body.cs:253` support the moved copy APIs. V2 also has a character-copy fallback. Test a new copy and an existing affected troop after reapplying gear; empty alternate outfits and unfinished equipment queues remain separate causes. |
| Nexus **1126485**, crafted weapons disappear after restarting; okyn121 / ImBlueShire, 1–2 Sep. [Report][bugs] | **Both — local match.** | Stable `Game/Wrappers/Item.cs:190`; V2 `Domain/Equipments/Wrappers/WItem.cs:367`: recognize persisted crafted IDs plus a weapon design when the runtime crafted flag is lost. Test with the reported smithing mods and a full restart. |
| Steam Servius / Berfis, 26 / 16 Aug; Nexus CommanderGrumbo, 24 Aug; tracker **1119047** title corroborates inverted UI. [Comments][steam] [Posts][posts] | **Stable — local match.** | `Utils/Version.cs` falls back to engine/API detection when the version file cannot be read. Verify the editor with the affected installation/total conversion. |
| Nexus **1120989**, inherited Vlandian kingdom keeps vanilla volunteers, 21 Aug. [Report][bugs] | **Stable — local match.** | `Game/BaseFaction.cs:45` distinguishes culture identity from faction identity; `Game/Wrappers/Notable.cs` avoids mistaking vanilla cultural troops for already-swapped kingdom troops. Test own-culture and conquered villages after inheritance. |
| Nexus Cerber991, 23–25 Aug, and Steam Grimlock, 21 Aug: Ironclad kills do not advance. [Posts][posts] [Comments][steam] | **Stable — local match.** | `Doctrines/Catalog/1_3_Ironclad.cs:53` excludes civilian outfits from the tier-six battle-armor check. Test eligible and ineligible battle outfits. |
| Nexus **1120488**, smithies/AI offer ballista arrows; hxll12 also reports this in posts on 29 Aug. [Report][bugs] | **V2 — local match.** | `Domain/Equipments/Wrappers/WItem.cs` rejects siege/practice ammunition; load cleanup checks equipped and queued items. The follow-up also fixes the separate rejection of ordinary throwing stones; the saved-ammunition regressions now pass. Full save/reload acceptance remains a live check. |
| Nexus **1123902**, V2 tournament crash; booquef, 22 Aug, identifies Arena Overhaul Redux. [Report][bugs] [Post][posts] | **V2 — local match.** | `Behaviors/Tournaments/Patches/TournamentParticipantPatch.cs:65` replaces excluded participants without shrinking the bracket. Test with custom-troop exclusion enabled and the reported arena mod. Stable's earlier tournament fix was already published in patch 30. |
| Nexus **1122787**, new campaign fails on game 1.5.1 with missing `get_OnCharacterCreationIsOverEvent`. [Report][bugs] | **V2 — local match for this exception.** | `Framework/Behaviors/CampaignEventsCompat.cs` adapts the event registration. The separate bodyguard deployment hook also adapts to 1.5. This does not establish compatibility with every 1.5 mod combination or explain every startup crash. |
| Nexus nightchill123, 17–26 Aug: player troops wait years for equipment in V2. [Post][posts] | **V2 — strong candidate.** | `Behaviors/Staging/StagedItemProgressBehavior.cs` and `Domain/Equipments/Models/MEquipment.Staging.cs` restore pending work and retain valid replacement progress. Test an existing queue across reload and Universal Editor use. |
| Nexus whatisthematrixneo, 24 Aug: newly crafted weapons require restart to appear. [Post][posts] | **V2 — local match.** | `Behaviors/Unlocks/CraftedItemsCacheBehavior.cs` invalidates the item caches on crafting. Distinct from weapons disappearing after restart. |
| Nexus RavenOne8452, 13 Aug: hundreds of duplicate crafted entries; AI carries four identical crafted weapons. [Post][posts] | **V2 — local match.** | `Editor/MVC/Pages/Equipment/Services/CraftedItemSelection.cs` groups unused designs while retaining stocked/equipped IDs; `ItemRandomizer.cs` excludes player-crafted AI picks; `AIClanRetinuesBehavior.EquipmentUpgrade.cs` avoids repeated weapons. Verify existing stock and loadouts survive. |
| Nexus AlexandersRage, 13 Aug: missing `scrub_save` in V2; author promises it on 22 Aug. [Post][posts] | **V2 — command request addressed; reported morale/tick crash remains a candidate.** | `Behaviors/Troops/StubIntegrityCheats.cs` adds the command. A command existing is not proof that the reported save corruption has been repaired. |

Related requests are also reflected in the patch: the 9999 total-skill setting ceiling (r9jeff, 27 Aug), two consecutive allied-army wins for stable Pragmatic Scavengers (Cerber991 / author reply, 18–22 Aug), and V2 `retinues.reset_doctrines` (mrsathletic, 24 Aug). These are feature/balance requests, not additional confirmed bug fixes. [Nexus posts][posts]

## Priority investigations not covered by a confirmed matching fix

### 1. V2 campaign freeze during daily troop training

**Nexus 1127726**, juzek666, 3 Sep: RoT campaigns slow down and then freeze; the supplied stack reaches `PartyBase.OnXpChanged`, `TroopRoster.AddXpToTroopAtIndex`, and daily training. [Report][bugs]

`Behaviors/Experience/Patches/ExperiencePatches.cs:171` and `:241` remain unchanged in the patch. The shared skill-point conversion fix is a different mechanism. A Harmony-generated method name in a stack does not identify which prefix, vanilla body, or other mod caused the exception.

Next reproduction: a copy of the affected pre-freeze save, exact game/RoT versions, the complete exception and mod list; isolate the failing party/roster element and its upgrade targets during a daily tick. Include prisoners, heroes, terminal troops and invalid custom references. Keep this separate from generic reload-corruption reports.

### 2. Stable battle crash when house guards are not infantry

**Nexus 1132876**, FightMeM80, 13 Sep, game 1.4.8 + RoT: initially attributed to archers, then narrowed to house guards in any non-infantry class. [Report][bugs]

No matching stable fix was found. `Features/Formations/Patches/FormationQuerySystemPatch.cs` is unchanged. V2's 1.5 bodyguard-hook adaptation is not a fix for this stable 1.4.8 report.

Next reproduction: identical guard data in infantry, ranged, cavalry and horse-archer variants; ordinary custom troops as controls; formation overrides on/off; mission entry and delegated command. Obtain the crash stack before assigning a cause.

### 3. V2 Royal Patronage progression

**Nexus 1133924**, knguiyen01, 15 Sep: Royal Stewardship and Royal Levy fail or behave inconsistently in V2. [Report][bugs]

**Stewardship null-handling defect fixed locally.** `Behaviors/Doctrines/Feats/Equipments/RoyalPatronage.RoyalStewardship.cs` used to dereference `s.Town.Governor` while iterating clan settlements, although villages have no Town component. It now skips these entries; the headless regression passes. This establishes a defect in the handler, not the cause of every symptom in the report. Positive companion eligibility, actual kingdom culture and feat prerequisites still need a campaign check.

**Levy event mismatch fixed in the second pass:** the player recruitment screen emits `OnUnitRecruited`; the feat listened to the separate `OnTroopRecruited` event. Both Royal Levy and Raise the Vanguard now use a distinct player hook. V2's value equality was already correct. Kingdom-versus-clan eligibility and an entire repeatable recruitment cycle still need campaign acceptance. Stable patch 31 already advertised its Royal Patronage correction, and an older reporter acknowledged using clan rather than kingdom troops. [Release discussion][posts]

### 4. Stable disabled Cultural Pride still runs its checks

**Nexus 1129461**, pka2077, 6 Sep, game 1.5.2: disabled equipment costs/doctrine still produce blocked-culture messages. [Report][bugs]

**Fixed locally:** `Doctrines/FeatServiceBehavior.cs` now excludes disabled doctrines when refreshing event handlers. Previously it filtered only by unlocked/prerequisite status, allowing Cultural Pride's blocked-culture messages despite its `IsDisabled` condition. The headless regression checks the real doctrine/feat models with costs enabled, disabled, re-enabled and a zero multiplier. Existing progress and saved keys are unchanged; the foreign-gear battle notification remains a live acceptance check.

Steam AndyMil's 14 Sep Empire 1700 cultural-feat report is related but not necessarily the same cause: verify the overhaul's item and troop cultures independently. [Steam comment][steam]

### 5. AI appears to modify ordinary cultural troop trees

**Nexus 1125711**, mrsathletic, 30 Aug, plus DeadMasterBxl, 23–25 Aug: V2 changes ordinary imperial units' equipment/class composition. [Report][bugs] [Discussion][posts]

The duplicate-weapon and player-crafted AI restrictions do not establish a fix. First distinguish an AI clan retinue from a vanilla cultural troop by object ID, then compare vanilla tree references and equipment before/after AI upgrades. The follow-up explicitly says the changes appear in the imperial tree, so do not dismiss this as intended AI-retinue behavior without checking.

### 6. External recruiters expose inactive custom stubs

**Nexus 1124993**, 29 Aug; Steam Habibi / Fraghuz, 2–3 Sep; older Nexus Improved Garrisons discussion: naked children or `retinues_custom_*` entries enter parties. [Report][bugs] [Steam replies][steam] [Nexus discussion][posts]

A Steam reply identifies a stale Convert Your Troops assignment to a stub. Treat that as a lead, not a universal diagnosis. Stub preservation/roster repair may help one path but does not ensure another mod filters inactive stubs. Test saved external assignments, troop-picker enumeration, removal/recreation of custom troops and reload. A separate Steam Improved Garrisons recruitment failure persisted with the FAQ's “All Lords Can Recruit” setting, so that setting alone does not close the report.

## Additional open or ambiguous cases

| Report / source | Assessment and targeted follow-up |
| --- | --- |
| **1126061**, 31 Aug, slow horse-archer house-guard replacement; ThorFreja, 7 Sep, only house guards arrive. [Bugs][bugs] [Posts][posts] | Stable auto-join checks caps, available party space and earned-renown reserve; there is no direct formation-class condition in `Features/AutoJoin/AutoJoinBehavior.cs`. Record these values and troop tier before calling this a class-specific bug. |
| **1122567**, 24–25 Aug, imported second clan branch absent from castle villages. [Bug][bugs] | Open. Town/castle recruitment selection needs a dedicated comparison. The custom-militia hook fix and inherited-kingdom fix do not prove this is covered. |
| **1117921**, 14 Aug, V2 custom kills do not unlock items. [Bug][bugs] | Partial candidate only. The local multiplier fix corrects weighted progress, not every zero-progress path. Test ordinary custom kills, player kills, allied kills, loss/win and doctrine multipliers separately. |
| DivineCarbon, 3 Sep, V2 regular/elite troops gain no daily training XP; Denlik1988, 14 Aug, GarrisonDrills retinue XP. [Posts][posts] | Separate earned skill points from roster upgrade XP. Check source settings and ownership. Current `ExperiencePatches.cs` intentionally excludes non-main-party training from skill-point awards; that does not explain a main-party failure. |
| **1116640**, white gear with TOAM, 11 Aug / 14 Sep reply. [Bug][bugs] | No matching color/banner fix. Compare heraldic and ordinary armor, item flags and banner data in a mission. |
| **1110202**, V2 widescreen controls; 1Arath1, 6 Sep, long item names hide harness button; cowork, 22 Aug, Captivity Events skills overflow. [Bug][bugs] [Posts][posts] | No matching layout fix. Test 21:9, long localized names and many custom skills. These differ from the inverted-editor version detection issue. |
| GiantMutantZ, 16 Aug: editing another hero changes the player face. [Post][posts] | Open, reported branch unspecified. Verify editor target identity and both heroes' body properties before/after confirm, cancel and reload. |
| **1100529**, BC275 naval convoy reinforcement inconsistency, 6 Jul. [Bug][bugs] | Open compatibility case. Naval battle detection changes do not modify convoy spawn/reinforcement composition. |
| Steam FleissigTozzen / Sir English, 7 Sep / 30 Aug: Bannerlord Together clients lose XP or templates. [Comments][steam] | No networking fix in either diff. Needs host/client persistence and authority tests; standalone save tests are insufficient. |
| Steam BurstingPixelz / Dirty and Nexus gamerlee94 / efebakar, 26–27 Aug: reload/map-tick crashes. [Steam][steam] [Nexus][posts] | Candidate for stable custom-stub preservation/canonical roster repair only if the stack/save confirms that mechanism. Do not merge all tick crashes into one issue. |
| spawn1993, 18 Aug: V2 kingdom creation and battle-exit crashes. [Post][posts] | Open without a stack. The author already asked for a report. Test kingdom creation separately from battle outcome/retreat. |
| saberdude123, 9 Sep: RoT “Other” trees still absent. [Post][posts] | Discovery was advertised in patch 31 / V2 patch 12. Verify installed version and tree eligibility; V2's new legacy-extra migration fix addresses a different import path. |
| babyopium00, 5 Sep: successful import leaves troops unchanged; Kitukatoo, 20 Aug: kingdom troops revert to clan upgrades; Thwiller, 13 Sep: recruits revert in RoT. [Posts][posts] | Open, branch/context incomplete. Capture source export, selected target faction/tree and before/after IDs. Do not presume the inherited-kingdom fix covers all three. |
| Damoose55 / ApolloX666: hidden doctrine/editor buttons; Finalwish8: unknown console commands. [Posts][posts] | Verify branch, exact command and conflicting UI mod. V2's map-bar compatibility fix was already shipped; new reset/scrub commands only address those specific missing commands. |
| ssssnake, 23 Aug: 5% recruitment setting but fully custom garrisons. [Post][posts] | Clarify regular custom troops versus actual retinues and recruitment ratio versus existing-garrison replacement. No matching patch identified. |
| Hiclipucli1 / Steam SteelBlood: javelin skirmisher classification; Steam BeanmanAgeofWoe / Meld: Engineering for guns. [Posts][posts] [Steam][steam] | Formation/skill support requests needing explicit expectations and overhaul fixtures, not verified regressions. |

## Already answered or previously released

- V2 raid XP inflation, enemy retinues looting despite a player victory, missing AI armor and AI using the player's retinues were addressed in the published 12 August patch. RavenOne8452 confirmed the earlier batch worked on 12 August. Do not count those older reports as new local-patch fixes. [Release replies][posts]
- Stable Royal Patronage, earlier tournament compatibility, gender issues and bandit-skill inflation already appear in the public patch 30/31 notes. New reports with similar symptoms still need version-specific verification. [Published changelog][posts]
- **1099321**, character-creation freeze with Mod Ready dependencies: a 9 July reply reports improvement after a dependency compatibility update. This is not evidence for a new Retinues fix. [Bug][bugs]
- Sanya123da resolved the horse-archer range/shooting complaint after changing skills/items; Micoll100799 reproduced startup failure with only dependencies enabled; sanjayraj85 reported a heraldic-armor compatibility workaround. Preserve those follow-ups rather than listing their original comments as confirmed Retinues defects. [Replies][posts]

## Tracker entries needing a later detailed read

The following New issue bodies did not load: **1101279** encyclopedia (V2.6), **1100103** transmog compatibility (V2.5), **1096034** elite-upgrade feat (stable .22), **1095914** crafted weapons (stable .28). No resolution is inferred from their titles.

Older Being looked at rows were surveyed by title: **1099660** Warlords Battlefield conversion sources, **1095862** gender, **1092109** Shokuho, **1032632** empty branches, **1041335** kingdom spawn frequency, **1014535** smithing persistence, **1015498** unspecified compatibility, **1011354** crossbow formations/F6 (November 2025). The last expansion also failed to load.

Need more info rows include **1119047** inverted UI; **1111138** V2 Defender of the City (body did not load); **1114658** battle-entry crash; **1108858** retinue/militia village leakage; **1019291**, **1040312**, **1060387**, **1026985** appearance issues; **1029247** battle loading; **1044879** kingdom troops; **1035734** duplicates; **1022758** variant equipment swaps. Five other rows are explicitly marked closed (**1078889**, **1023338**, **1016061**, **1019726**, **1020405**). These titles alone are insufficient to mark any report resolved. [Tracker][bugs]

## Suggested test order

1. Preserve a pre-test save and record exact mod/game versions. Reproduce V2 daily-training freezes and stable non-infantry guard battle crashes first.
2. Exercise V2 Royal Patronage with owned villages, companion governors and genuine kingdom recruits; exercise stable disabled-doctrine event dispatch.
3. Test AI upgrades and external recruiter integrations while checking vanilla object IDs, inactive stubs and existing roster references.
4. Verify each local-match scenario above with an existing affected save, then save, restart and load again. Check crafted stock, queued items and alternate outfits explicitly.
5. Run UI overflow/hero-edit tests and the remaining feature-specific cases. Keep co-op as a separate integration matrix.

The throwing-stone preservation regression is fixed and passes in the follow-up matrix. The campaign-test cleanup failure remains pending, and the full installed mod-list startup crash is not isolated. These are independent of the public report triage. See [live-game matrix](LIVE_GAME_MATRIX.md), [save compatibility](SAVE_COMPATIBILITY.md), and [validation notes](VALIDATION.md).

[workshop]: https://steamcommunity.com/sharedfiles/filedetails/?id=3599557394
[steam]: https://steamcommunity.com/sharedfiles/filedetails/comments/3599557394
[bugs]: https://www.nexusmods.com/mountandblade2bannerlord/mods/8847?tab=bugs
[posts]: https://www.nexusmods.com/mountandblade2bannerlord/mods/8847?tab=posts
