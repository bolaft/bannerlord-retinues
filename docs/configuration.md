---
title: Mod Configuration
nav_order: 9
---

# Mod Configuration

You can tune Retinues from the **Mod Configuration Menu (MCM)**. Options are grouped like the in-game sections below. Three global presets exist:

- **Default:** balanced first playthrough.
- **Freeform:** very permissive, fast prototyping.
- **Realistic:** stricter rules, slower progression, more immersion.

> ⚠️ If an option is marked “*(restart)*”, return to main menu (or start a new game) after changing it.

---

## Retinues

Limits and costs for **converting** troops into retinues and **advancing** (ranking up) retinues.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Max Elite Retinue Ratio** | Max **fraction of your party size limit** that may be **Elite Retinues** (e.g., 0.10 with a party size 200 → up to 20 elites). Affects **auto-hire** and **manual conversion** (you cannot exceed the cap). | 0.10 | 1.00 | 0.05 |
| **Max Basic Retinue Ratio** | Same as above, but for **Basic Retinues**. Basic and Elite have **separate caps** and are checked independently. | 0.20 | 1.00 | 0.10 |
| **Retinue Conversion Cost Per Tier** | **Gold** charged **per unit tier** when **manually converting** a custom/vanilla unit into a retinue (instant method). Example: T4 unit with cost=50 → 200 gold. | 50 | 0 | 100 |
| **Retinue Rank Up Cost Per Tier** | **Gold** charged **per retinue tier increase** (Rank Up). May also require sufficient **XP** and skill caps met. | 1000 | 0 | 1000 |
| **Restrict Retinue Conversion To Fiefs** | If **On**, you may **manually convert** to retinues **only while in a (your) settlement/fief**. If **Off**, conversion can be done anywhere the editor allows. | Off | Off | On |

> Doctrines (e.g., cap boosters) and your party size both influence how many retinues you can field.

---

## Recruitment

Control where/how custom volunteers appear and who can recruit them across the world.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Volunteer Swap Proportion** | Fraction **0.0–1.0** of vanilla volunteer slots replaced by your **custom** volunteers in eligible settlements. **1.0** = fully swapped; **0.5** = half of the volunteers are custom (rounded by slot). | 1.0 | 1.0 | 1.0 |
| **Recruit Clan Troops Anywhere** | If **On**, the player can recruit **clan** custom troops in **any** settlement (not only “owned/affiliated”). If **Off**, recruitment is more restrictive/organic. | Off | On | Off |
| **Swap Volunteers Only For Correct Culture** | If **On**, a settlement's volunteers are swapped **only if the settlement culture matches** the troop's culture. If **Off**, swaps can happen regardless of culture. | Off | Off | On |
| **Clan Troops Over Kingdom Troops** | When both **clan** and **kingdom** trees are available, prioritize **clan** volunteers for settlements tied to you. Turning **Off** prioritizes kingdom lines. | On | Off | On |
| **No Kingdom Troops** | If **On**, disables the **kingdom** layer entirely (only **clan** troops exist). Useful for simpler runs. | Off | Off | Off |
| **Vassal Lords Recruit Custom Troops** | If **On**, **your vassals** can recruit your custom troops (world simulation picks from your trees for their parties). | On | On | On |
| **All Lords Recruit Custom Troops** | If **On**, **every AI lord** (not only your vassals) may recruit from your custom trees where rules allow. | Off | Off | On |

---

## Restrictions

When you may edit troops, and whether extra appearance controls are exposed.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Restrict Editing To Fiefs** | If **On**, most **editor actions** (renaming, skills, upgrade targets, equipment) require being **inside a settlement/fief**. | Off | Off | On |
| **Experimental: Enable Appearance Controls** | If **On**, exposes **age / height / weight / build** sliders in addition to the **gender** toggle. **Experimental**: may cause visual oddities with some armors/cultures. | Off | Off | Off |

---

## Doctrines

Gates and strictness for the doctrine/feat system.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Enable Doctrines** *(restart)* | Master switch for the **Doctrines & Feats** system. Requires reload/new game if changed. | On | On | On |
| **Disable Feat Requirements** | If **On**, doctrines **ignore** feat prerequisites (unlock freely). If **Off**, you must **progress feats** to unlock power. | Off | On | Off |

---

## Equipment

Money/time rules, availability filters, culture/crafted rules, and mounts.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Pay For Troop Equipment** | If **On**, equipping an item without **stock** purchases it for **gold** using **Equipment Price Modifier**. If **Off**, no gold cost to equip. | On | Off | On |
| **Equipment Price Modifier** | Multiplier over item **value** when buying via the editor (e.g., 2.0 means pay **2× item value**). | 2.0 | 0.0 | 4.0 |
| **Changing Troop Equipment Takes Time** | If **On**, changes are **staged** and resolve after a **time delay** (must confirm in town via “Equip troops”). If **Off**, equip applies instantly (subject to other rules). | Off | Off | On |
| **Equipment Change Time Modifier** | Scales the **hours** needed for staged equipment changes. Higher = slower. | 2 | 2 | 4 |
| **Restrict Items To Town Inventory** | If **On**, only items **sold in the current town** are **available** to equip (even if unlocked). If **Off**, unlocked items are usable anywhere. | Off | Off | On |
| **Allowed Tier Difference** | Max allowed difference between **troop tier** and **item tier/difficulty**. If exceeded, the item is blocked until the troop ranks up or a doctrine loosens the rule. | 3 | 6 | 2 |
| **Force Main Battle Set In Combat** | If **On**, the **main Battle** set is forced during missions (ignores alternates). Useful for simplicity or compatibility. | Off | Off | Off |
| **Ignore Civilian Horse for Upgrade Requirements** | If **On**, civilian horse **does not count** toward mounted upgrade checks (prevents weird gating via civilian kits). If **Off**, civilian horses can satisfy mount requirements. | On | On | Off |
| **Disable Crafted Weapons** | If **On**, **crafted** designs are **excluded** from the equipment lists. If **Off**, the game can show the **latest crafted variant per design**. | Off | Off | On |
| **Disallow Mounts For Tier 1** | If **On**, **T1 troops** cannot equip **horse/harness** (slots disabled). If **Off**, T1 can mount if other rules allow. | On | Off | On |

> Doctrines can also modify costs/availability (e.g., **Royal Patronage** gives rebates for kingdom-culture gear; **Ancestral Heritage** and **Clanic Traditions** affect culture/crafted availability).

---

## Skills (Training)

Time and XP economy for skill increases; pool behavior.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Troop Training Takes Time** | If **On**, raising skills consumes **in-game hours** via the training flow (Wait/Train). If **Off**, skill increments apply immediately (XP permitting). | Off | Off | On |
| **Training Time Modifier** | Scales **hours per training session**. Higher = longer training periods before points apply. | 2 | 2 | 3 |
| **Base Skill XP Cost** | Baseline **XP** required to buy the **next skill point** (before slope). | 100 | 0 | 200 |
| **Skill XP Cost Per Point** | Linear **slope** added per current skill value (makes later points costlier). Example: nextCost = base + (currentValue × slope). | 1 | 0 | 2 |
| **Shared XP Pool** | If **On**, all custom troops draw from a **single shared XP pool**. If **Off**, each troop has its **own pool**. | Off | On | Off |

---

## Unlocks

How gear gets unlocked into your equipment lists during play.

| Option | What it does (detail) | Default | Freeform | Realistic |
|---|---|---:|---:|---:|
| **Unlock From Kills** | If **On**, **played battle** victories add unlock **progress** for the **exact items** seen on defeated enemies. | On | Off | On |
| **Required Kills For Unlock** | Progress **threshold** (count) per item to fully unlock via kills. Higher = slower. | 100 | 100 | 200 |
| **Unlock From Discarded Items** | If **On**, discarding items adds unlock **progress** toward that item's threshold. | On | Off | On |
| **Required Discarded Items For Unlock** | **Count** of discards needed to unlock via discard route. | 10 | 10 | 20 |
| **Own Culture Unlock Bonuses** | If **On**, items matching your **clan/kingdom culture** gain **bonus progress** (and sometimes share progress across similar items), speeding up culture-faithful kits. | On | On | Off |
| **Unlock From Culture** | If **On**, anything of your **troop's culture** is considered **available** (ignores kill/discard requirements). | Off | On | Off |
| **All Equipment Unlocked** | If **On**, every item is **available** (ignores unlock rules). Great for testing/sandbox. | Off | On | Off |

> With *Restrict Items To Town Inventory* also On, an item can be unlocked but still unavailable if the current town doesn't sell it.

---

## Skill Caps

Per-tier maximum value per skill. Retinues get an extra flat bonus on top. Doctrines can add more.

- **Retinue Skill Cap Bonus:** +5 (Freeform: +50, Realistic: +0).  
- **Tier caps (defaults):**  
  - **T0** 20, **T1** 20, **T2** 50, **T3** 80, **T4** 120, **T5** 160, **T6** 260, **T7+** 360.  
  - All caps can be raised up to 360. Freeform sets all to 360.

**What it means:** a T3 troop with cap 80 cannot take a skill to 100 unless the cap is raised (by tiering up, retinue bonus, doctrines, or preset).

---

## Skill Totals

Per-tier total points you may distribute across all eight combat skills. Retinues get an extra flat bonus.

- **Retinue Skill Total Bonus:** **+10** (Freeform: +100, Realistic: +0).  
- **Tier totals (defaults):**  
  - **T0** 90, **T1** 90, **T2** 210, **T3** 360, **T4** 535, **T5** 710, **T6** 915, **T7+** 1600.  
  - **Freeform** sets every tier to **1600**.

**What it means:** totals are a budget that caps how many points you can spend overall, even if individual skill caps would allow more.

---

## Debug

- **Debug Mode:** Prints verbose logs to help diagnose behavior (imports/exports, unlock ticks, training, etc.). Turn off for normal play.
