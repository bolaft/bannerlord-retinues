---
title: Equipment & Unlocks
nav_order: 6
---

# Equipment & Unlocks

Customize **battle**, **civilian**, and optional **alternate** loadouts for every custom troop (including retinues) from the **Clan → Troops → Equipment** editor.

---

## Loadout Types

- **Battle:** the equipment set used in most combats.
- **Civilian:** worn in towns/villages and non-combat scenes.
- **Alternate Sets:** optional extra battle sets you can enable per battle type.

Use the **left/right arrows** near the set name to switch sets. You can **create** or **remove** alternate sets (unless a conflicting mod disables it).

You can also **toggle** whether an alternate set is to be used selectively in **Field Battles**, **Siege Defense**, or **Siege Assault**. The checkboxes are located just below the equipment set selection buttons.

> Removing a set **unequips and stocks** all its items and clears pending changes.

---

## Slots

Each set exposes these slots:
**Head, Cape, Body, Gloves, Legs, Weapon 1–4, Horse, Harness**.

- **Harness** requires a **Horse** equipped.
- If **No mounts for Tier 1** is enabled (MCM), **Horse**/**Harness** are disabled for T1 troops.

---

## Item Availability & Unlocks

What appears in the item list depends on your **rules** and **progress**:

### Global unlocks (MCM)
- **All Equipment Unlocked:** see everything.
- **Restrict to Town Inventory:** only items sold in the current settlement are marked available.
- **Unlock from Culture:** items matching the troop's culture are available.
- **Unlock from Kills:** defeating enemies with an item progresses its unlock. At the threshold, it becomes unlocked.
- **Unlock from Discards:** discarding items in inventory adds progress toward the threshold.
- **Own-culture bonuses:** your clan culture can earn bonus unlock progress from other cultures' unlocks.

### Doctrines that influence availability
- **Clanic Traditions:** enables crafted weapons to appear (only latest variant per design).
- **Ancestral Heritage:** items of your clan/kingdom culture can be treated as available.
- **Pragmatic Scavengers / Battlefield Tithes / Lion's Share:** modify which kills count (see *Unlocks from kills* below).

### Unlocks from kills (played battles)
At the end of a victorious battle you played, the game counts equipped items found on defeated enemies and adds progress toward unlocking those items. Doctrine effects:
- **Pragmatic Scavengers:** also count ally casualties.
- **Battlefield Tithes:** allow allied troop kills to contribute.
- **Lion's Share:** player kills count double.

A modal summary pops up when new items unlock.

> In the list, locked items show *progress %* and are disabled until fully unlocked. With *Restrict to Town Inventory*, an item can be unlocked but still unavailable in the current town.

---

## Stocks (Own Supply)

The editor shows **In Stock (n)** for items you own. Equipping from stock does *not* charge gold. You can edit stocks via the console (see [Cheats](./cheats.md)).

---

## Costs, Rebates & Purchases

If *Pay for Equipment* is on, items without stock have a gold cost:
- Base cost = item value × *Equipment Price Modifier* (MCM).
- **Royal Patronage** doctrine: **-10%** cost for items of your kingdom's culture.

If you have enough gold, you'll get a buy confirmation; confirm to purchase and equip.

---

## Staging vs Instant Equip

You can run in two styles (MCM):

- **Instant Equip ON**  (default) → changes apply *immediately* in the editor (still honoring context rules).

- **Instant Equip OFF** → **Equipment changes take time**  
  Selections become *pending* (a small timer appears, e.g., “(3h)”). To apply them:
  1. Travel to a fief (town).
  2. Use the town menu action *“Equip troops.”*  
  Pending changes then resolve, swapping the actual gear.

> Some actions may be only allowed in settlements (the UI will explain if something is blocked).

---

## Requirements & Blocks

An item row shows why it's disabled, for example:

- **Not in town:** restricted by *Restrict to Town Inventory* and the settlement doesn't sell it.
- **Unlocking (x%):** progress from kills/discards hasn't reached the threshold yet.
- **Skill requirement:** troop's skill is below the item's **difficulty** in its **relevant skill**.
- **Tier too high:** difference to troop **Tier** exceeds your *Allowed Tier Difference* (MCM). The **Ironclad** doctrine relaxes this.
- **Not civilian:** trying to equip a non-civilian item into the Civilian set.

Hovering an item displays its stats.

---

## Sorting & Filtering

Use the list header to sort by **Name**, **Category**, **Tier**, or **Cost** (ascending/descending). A **search box** filters by name, category, type, or culture.

> For performance, the list caps at **1000** rows. Your currently equipped and staged items are always kept visible even if beyond the cap.

---

## Horses & Upgrades

- Unequipping a horse** automatically unequips the harness.
- For upgrade requirements, the system looks at the best horse category across your sets (battle/civilian/alternates) and may use it to gate upgrades to certain mounted units. By default the *Ignore Civilian Horse* option is enabled and the civilian set's horse will be excluded from upgrade requirements.
