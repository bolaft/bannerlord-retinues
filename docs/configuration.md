---
title: Mod Configuration
nav_order: 9
---

# Mod Configuration

You can tune Retinues from the **Mod Configuration Menu (MCM)**. Options are grouped like the in-game sections below. Three global presets exist:

- **Default:** balanced first playthrough.
- **Freeform:** very permissive, fast prototyping.
- **Realistic:** stricter rules, slower progression, more immersion.

> ⚠️ If an option is marked "*(restart)*", return to main menu (or start a new game) after changing it.
>
> 🧹 If options don't seem to apply correctly or you get strange behavior after updating Retinues, clear MCM's cached settings for the mod:  
>   1. Close the game.  
>   2. Delete the `Retinues` folder in  
>      `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\ModSettings\`  
>      (for example: `C:\Users\YourName\Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Retinues`).  
>   3. Restart the game and re-apply your options in MCM.

---

## Retinues

Limits and costs for converting troops into retinues and advancing (ranking up) retinues.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Max Elite Retinue Ratio** | Max **fraction of your party size** that can be **elite retinues** (0.0–1.0). Checked against party size when converting/adding a retinue. | 0.10 | 1.00 | 0.05 |
| **Max Basic Retinue Ratio** | Same as above, but for **Basic Retinues**. Elite and Basic ratios are separate caps and are checked independently. | 0.20 | 1.00 | 0.10 |
| **Gold Conversion Cost (Per Tier)** | **Gold** charged **per tier of the retinue troop** when converting a regular troop into a retinue. Example: T4 retinue with cost=50 → 200 gold. | 50 | 0 | 100 |
| **Rank Up Cost (Per Tier)** | **Gold** charged **per retinue tier** when ranking up a retinue troop. Rank-ups also require sufficient **XP** and skill caps. | 1000 | 0 | 2000 |
| **Influence Conversion Cost (Per Tier)** | **Influence** cost **per tier of the retinue troop** when converting a regular troop into a retinue. | 5 | 0 | 10 |
| **Renown Required (Per Tier)** | **Clan renown** required for a retinue to join automatically, **per tier** of the retinue troop. Higher values delay access to high-tier retinues. | 10 | 10 | 20 |

> Doctrines and your party size both influence how many retinues you can practically field.

---

## Recruitment

Control where/how custom volunteers appear and who can recruit them across the world.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Custom Volunteer Proportion** | Fraction **0.0–1.0** of vanilla volunteers that are **replaced by custom troops** in eligible settlements. `1.0` = all volunteers become custom, `0.5` ≈ half of them. | 1.0 | 1.0 | 1.0 |
| **Restrict To Owned Settlements** | If **On**, custom troops can only appear in **settlements owned by the player's clan or kingdom**. If **Off**, custom volunteers can also appear in neutral/other-faction fiefs. | On | Off | On |
| **Restrict To Same Culture Settlements** | If **On**, volunteers in **settlements of a different culture** will *not* be replaced by custom troops. If **Off**, swaps can happen regardless of settlement culture. | Off | Off | On |
| **Kingdom Volunteers In Clan Fiefs Proportion** | Chance **0.0–1.0** for each volunteer in **player clan fiefs** to be a **kingdom troop** instead of a clan/custom troop. Use this to re-introduce kingdom troops in your own fiefs. | 0.0 | 0.0 | 0.0 |
| **Clan Volunteers In Kingdom Fiefs Proportion** | Chance **0.0–1.0** for each volunteer in **kingdom-owned fiefs** to be a **clan/custom troop** instead of a pure kingdom troop. | 0.0 | 0.0 | 0.0 |
| **Vassal Lords Recruit Custom Troops** | If **On**, lords of the **player's clan or kingdom** can recruit custom troops in their fiefs. May be auto-locked **On** when daily volunteer swap is forced by another mod. | On | On | On |
| **All Lords Recruit Custom Troops** | If **On**, **any lord** can recruit custom troops in the player's fiefs (required for some garrison mods like Improved Garrisons). May be auto-locked **On** by compatibility. | On | On | On |

---

## Troop Unlocks

Rules for when special troop templates become available at all (separate from equipment unlocks).

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **No Fief Requirements** | If **On**, troops can be **unlocked without owning a fief**. If **Off**, some troops require you to control a fief first. | Off | On | Off |
| **No Doctrine Requirements** | If **On**, **special troops** (militias, villagers, caravan guards, etc.) can be acquired **without owning the corresponding doctrines**. | Off | On | Off |
| **Disable Kingdom Troops** | If **On**, the **kingdom troop tree is disabled** and **clan troops** are always used instead. Useful for runs focused entirely on custom clan lines. | Off | Off | Off |
| **Copy All Sets On Unlock** | If **On**, when unlocking a new troop template, **all equipment sets** of the original troop (battle, civilian, alternates) are copied instead of only main battle + civilian. | Off | On | Off |

---

## Global Troop Editor

Options controlling the escape-menu **Global Troop Editor**.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Enable Global Troop Editor** *(restart)* | Master switch for the **Global Troop Editor** (escape menu). When **Off**, only your own faction troops can be edited. Changing this requires going back to the main menu or starting a new campaign. May be auto-disabled if another mod is incompatible. | On | On | On |
| **Keep Upgrade Requirements For Vanilla Troops** | If **On**, vanilla troop **upgrade requirements** (e.g. war horses) are **preserved** when you change their equipment instead of being recalculated from the new loadouts. | On | On | On |

---

## Restrictions

When you may edit troops, and how wide upgrade trees can branch.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Restrict Editing To Fiefs** | If **On**, most **editor actions** (conversions, equipment changes, training) require being **inside a settlement/fief**. If **Off**, many actions are allowed from the campaign map. | Off | Off | On |
| **Max Elite Upgrades** | Maximum number of **upgrade targets** a single **elite troop** can have (branching). Higher values allow wider trees and more elite paths. | 1 | 4 | 1 |
| **Max Basic Upgrades** | Maximum number of **upgrade targets** a single **basic troop** can have. Higher values allow wider branching for basic lines. | 2 | 4 | 2 |

---

## Doctrines

Gates and strictness for the doctrine/feat system.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Enable Doctrines** *(restart)* | Master switch for the **Doctrines** system and its bonuses. **Saving with doctrines disabled will clear all doctrine data in the save.** | On | On | On |
| **Enable Feat Requirements** *(restart)* | If **On**, doctrines require feats to be completed before unlocking. If **Off**, feat requirements are ignored and doctrines can be unlocked freely. **Saving with feats disabled will clear all feat data in the save.** | On | Off | On |
| **Doctrine Gold Cost Multiplier** *(restart)* | Multiplier applied to **doctrine gold costs**. `0.0` = free doctrines, `2.0` = double cost. | 1.0 | 0.0 | 1.0 |
| **Doctrine Influence Cost Multiplier** *(restart)* | Multiplier applied to **doctrine influence costs**. Works like the gold multiplier but for influence. | 1.0 | 0.0 | 1.0 |

---

## User Interface & Appearance

Visual/UI extras that don’t affect core balance but change how you interact with the editor.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Enable Editor Hotkey (Shift + R)** | If **On**, enables the **Shift + R** hotkey on the campaign map to open the Retinues editor directly. | Off | Off | Off |
| **Enable Item Comparison Icons** | If **On**, adds **comparison icons** (better/worse/equal) next to items in the equipment list relative to the currently equipped item. | On | On | On |
| **Enable Appearance Controls** | If **On**, shows **age / height / weight / build** sliders in the editor for troops and heroes. These are **cosmetic only** and may occasionally create visual oddities with some armors/cultures. | On | On | On |
| **Max Equipment Rows Per Page** | Maximum number of **equipment rows** to show per page in the troop editor. Lower values can improve UI responsiveness on lower-end machines or with huge item lists. | 100 | 100 | 100 |

---

## Equipment

Money/time rules, availability filters, culture rules, mounts, and formation behavior.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Equipping Troops Costs Gold** | If **On**, equipping an item without **stock** purchases it for **gold**, scaled by **Equipment Price Modifier**. If **Off**, no gold cost to equip. | On | Off | On |
| **Equipment Price Modifier** | Multiplier over item **value** when buying missing gear for troops. Example: `2.0` means paying **2× item value**. | 2.0 | 0.0 | 4.0 |
| **Changing Troop Equipment Takes Time** | If **On**, changes are staged and apply only after troops spend **in‑game hours** upgrading equipment in a fief (Wait/Upgrade). If **Off**, equipment applies instantly (subject to other rules). | Off | Off | On |
| **Equipment Time Multiplier** | Scales the **hours** needed for staged equipment changes. Higher = slower. | 2 | 2 | 4 |
| **Restrict Items To Town Inventory** | If **On**, only items **currently sold in the settlement** are allowed for troop equipment (even if unlocked). If **Off**, any unlocked item can be used regardless of local shops. | Off | Off | On |
| **Allowed Tier Difference** | Max allowed difference between **troop tier** and **item tier** at equip time. Prevents low-tier troops from wearing top-tier gear until they rank up or doctrines loosen rules. | 3 | 6 | 2 |
| **Force Main Battle Set In Combat** | If **On**, troops always spawn with their **main battle set** in combat, ignoring alternate sets. Useful for simplicity or compatibility if alternate sets cause issues. | Off | Off | Off |
| **No Civilian Set Upgrade Requirements** | If **On**, **horses in civilian sets are ignored** when checking mount requirements for upgrades. Only battle-set horses count for mount requirements. | On | On | Off |
| **No Noble Horse Upgrade Requirements** | If **On**, upgrades never specifically require **noble horses**; a normal war horse is always enough. | On | On | Off |
| **Disallow Mounts For T1 Troops** | If **On**, **T1 troops** cannot have mounts, even if other rules would allow it. If **Off**, T1 can mount if other conditions are met. | On | Off | On |
| **Allow Formation Overrides** | If **On**, allows **manual overriding of troop formation class**. May cause awkward AI behavior and can slow down the pre‑battle formation screen, so leave **Off** unless you know you need it. | Off | Off | Off |

---

## Skills (Training)

Time and XP economy for skill increases; pool behavior.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Training Troops Takes Time** | If **On**, raising skills consumes **in‑game hours** via the training flow (Wait/Train). If **Off**, skill increments apply immediately (XP permitting). | Off | Off | On |
| **Training Time Multiplier** | Scales **hours per training session**. Higher = longer training periods before points apply. | 2 | 2 | 3 |
| **Skill XP Cost (Base)** | Baseline **XP** required to buy the **next skill point** (before slope). | 100 | 0 | 200 |
| **Skill XP Cost (Per Point)** | Linear **slope** added per current skill level. Example: `nextCost = base + (currentValue × slope)`. | 1 | 0 | 2 |
| **Shared XP Pool** | If **On**, all edited troops share a **single XP pool** instead of having individual XP. If **Off**, each troop has its **own pool**. | Off | Off | Off |
| **Force XP Refunds** | If **On**, when lowering a troop’s skill, the XP previously spent on those points is **always refunded** to the pool. If **Off**, decreasing skills does not refund XP. | Off | Off | Off |

---

## Equipment Unlocks

How gear gets unlocked into your equipment lists during play.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **All Equipment Unlocked** | If **On**, every item is **available from the start**, ignoring all other unlock rules. Great for testing/sandbox. | Off | On | Off |
| **Unlock Items From Kills** | If **On**, **played battle** victories unlock gear based on the **items worn by defeated enemies**. | On | Off | On |
| **Required Kills Per Item** | Progress **threshold** (count) of eligible enemies that must be defeated to unlock a specific item via kills. Higher = slower. | 100 | 100 | 200 |
| **Unlock Items From Discards** | If **On**, **discarding items** contributes unlock progress toward that item's threshold. | Off | Off | Off |
| **Required Discards Per Item** | **Count** of discards needed to unlock an item via the discard route. | 10 | 10 | 20 |
| **Player Culture Unlock Bonus** | If **On**, item unlock progress also adds progress to **random items matching the player culture**, making culture-faithful kits unlock faster. | On | On | Off |
| **All Culture Equipment Unlocked** | If **On**, **player clan and kingdom culture items** are always available, regardless of kill/discard progress. | Off | Off | Off |
| **Unlock Popup** | If **On**, displays a **popup notification** when items are unlocked. If **Off**, unlocks only appear in the log. | On | On | Off |

> With *Restrict Items To Town Inventory* also On, an item can be unlocked but still unavailable if the current town doesn't sell it.

---

## Skill Caps

Per-tier maximum value per skill. Retinues get an extra flat bonus on top. Doctrines can add more.

- **Retinue Skill Cap Bonus:** +5 (Freeform: +50, Realistic: +0).  
- **Tier caps (defaults):**  
  - **T0** 20, **T1** 20, **T2** 50, **T3** 80, **T4** 120, **T5** 160, **T6** 260, **T7+** 360.  
  - All caps can be raised up to 360. Freeform sets all to 360.
- **Hero Skill Cap:** 420 (Freeform: 420). Maximum per-skill cap for heroes.

**What it means:** a T3 troop with cap 80 cannot take a skill to 100 even if you have the XP; the cap must be raised (by tiering up, retinue bonus, doctrines, or preset).

---

## Skill Totals

Per-tier total points you may distribute across all eight combat skills. Retinues get an extra flat bonus.

- **Retinue Skill Total Bonus:** **+10** (Freeform: +100, Realistic: +0).  
- **Tier totals (defaults):**  
  - **T0** 90, **T1** 90, **T2** 210, **T3** 360, **T4** 555, **T5** 780, **T6** 1015, **T7+** 1600.  
  - **Freeform** sets every tier to **1600**.

**What it means:** totals are a **budget** that caps how many points a troop can spend overall, even if individual skill caps would allow more.

---

## Debug

- **Debug Mode:** Prints verbose logs to help diagnose behavior (editor usage, imports/exports, unlock ticks, training, etc.). Turn off for normal play.

