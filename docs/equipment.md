---
title: Equipment & Unlocks
nav_order: 6
---

# Equipment & Unlocks

Customize battle and civilian loadouts for every custom troop (including retinues) from the **Clan → Troops → Equipment** editor.

You can have multiple battle sets and multiple civilian sets per troop. Each set is equivalent; you decide which ones are used in which situations.

---

## Loadout Types & Sets

Each custom (non-hero) troop has:

- One or more Battle sets  
  Used in combat. You can enable/disable each battle set separately for:
  - Field Battles
  - Siege Defense
  - Siege Assault

- One or more Civilian sets  
  Used in towns, villages, and non-combat scenes.

The editor always enforces:

- At least one battle set must exist.
- At least one civilian set must exist.
- For each battle type (field / siege defense / siege assault), there must be at least one battle set enabled.
- The first set is always a battle set and cannot be flipped to civilian.

---

## Managing Sets

At the top of the equipment panel, you'll see controls like:

- **Set number** (e.g. `1`, `2`, `3`…)
- **Previous / Next** arrows: switch which set you are editing.
- **Create Set** button
- **Remove Set** button
- **"Civilian" toggle**: flips a set between Battle and Civilian.
- **Battle-type toggles** (for battle sets only):
  - "Field"
  - "Siege Defense"
  - "Siege Assault"

### Creating Sets

When you create a new set (non-heroes only), you're asked:

- **Copy Current:** duplicates the current battle set into a fresh one.
- **Empty:** creates a completely empty battle set.

Creating sets is **always free**:

- No gold cost.
- No stock consumed.
- The new set simply starts with copied items (or empty slots).

New battle sets start disabled for all battle types; the game will auto-fix combat policies so each battle type still has at least one enabled set.

### Removing Sets

You can remove any extra set as long as removing it would still leave at least one battle set and at least one civilian set overall.

When you remove a set:

- Only the items extra copies that were needed because of this specific set are refunded to your stock.
- Items that are still required by other sets are kept "in use" and not refunded.

### Flipping a Set to Civilian/Battle

The Civilian toggle flips the current set.

Rules:

- The first set of a custom troop must remain battle.
- You must always keep at least one battle and one civilian set after the flip.

If you flip a battle set to civilian and it contains non-civilian items:

- You'll get a warning popup.
- Confirming will unequip all non-civilian items from that set, refunding the freed copies to your stocks.
- Then the set becomes civilian.

---

## Slots

Each set exposes these slots:  
**Head, Cape, Body, Gloves, Legs, Weapon 1–4, Horse, Harness**.

- **Harness** requires a **Horse** equipped.
- If **"No mounts for Tier 1"** is enabled (MCM), **Horse**/**Harness** are disabled for T1 troops.

---

## Shared Items Across Sets

Items are treated as shared objects across all sets:

- The game tracks how many copies of each item are needed in the "busiest" single set.
- If you equip the same item in multiple sets, you only pay/stock enough copies for that maximum per set, not "number of sets × slots".

In the list:

- If an item is already fully owned for another set, the row shows it as "available from another set" (checkmark).
- Equipping such an item into the current set is free and does not consume or buy additional copies.

When you unequip items or remove a set, only the extra copies that stop being needed are refunded to your stock.

---

### Captains & Equipment

When the **Captains** doctrine is unlocked and a troop has a Captain variant:

- The **base troop** and its **Captain** effectively share their equipment usage:
  - Any item currently worn by the base troop is treated as available for the Captain at **no extra cost**.
  - Any item currently worn by the Captain is treated as available for the base troop at **no extra cost**.
- In practice, swapping items between a troop and its Captain behaves like equipping from a shared pool: if one already uses the item, equipping it on the other is free and does not require buying extra copies.

---

## Stocks (Own Supply)

The editor shows **"In Stock (n)"** for items you own.

- Equipping from stock does not charge gold.
- Shared-item rules still apply: the system only cares about the maximum copies needed per set.
- You can edit stocks via the console (see [Cheats](./cheats.md)).

---

## Costs, Rebates & Purchases

If **Pay for Equipment** is enabled:

- Items without enough stock have a gold cost:
  - Base cost = item value × **Equipment Price Modifier** (MCM).
  - **Cultural Pride** doctrine: -20% cost for items of your clan's culture.
  - **Royal Patronage** doctrine: -20% cost for items of your kingdom's culture.
- Before buying, you'll see a confirmation popup if gold is required.
- The system:
  - Consumes as many copies as possible from stock.
  - Buys the missing copies, adds them to stock, then equips them.
  - Only buys up to the number of extra copies actually needed according to the shared-item rule.

Refunds:

- When you unequip or delete sets and fewer copies are now required, the extra physical copies are returned to stock.
- If you cancel a staged change (see below), all stock changes made for that change are refunded.

---

## Staging vs Instant Equip

You can run equipment changes in two styles (MCM):

### Instant Equip ON (default)

- Changes apply immediately in the editor.
- You still respect context rules (some actions only allowed in settlements).

### Instant Equip OFF → Equipment Changes Take Time

In this mode:

- Equipping a new item creates a pending (staged) change instead of immediately changing the troop's live gear.
- A small timer appears (e.g. "(3h)") on staged slots.

To apply all staged changes:

1. Travel to a fief (town).
2. Use the town menu action **"Equip troops"**.  
   All pending changes resolve in one go, updating the actual gear.

**Unequipping is always instant** (never staged), but:

- If unequipping would reduce required copies in a way that matters for staged logic, you may see a small warning explaining that equipping something different later may take time.

You can also:

- **Reset Changes:** cancels all staged changes for the current set and fully refunds any stock/gold adjustments those changes had already made.

---

## Item Availability & Unlocks

What appears in the item list depends on your rules and progress.

### Global Unlock Options (MCM)

- **All Equipment Unlocked**  
  Shows all equippable items; unlock progress is ignored.

- **Restrict to Town Inventory**  
  - Item rows are grayed out when the item is not sold in the current settlement.
  - An item can be unlocked but still unavailable here if this town doesn't sell it.

- **Unlock from Culture**  
  - Items matching the troop's faction culture are considered available even if you haven't unlocked them through kills/discards yet.

- **Unlock from Kills**  
  - Defeating enemies with an item progresses its unlock counter.
  - Once it hits the threshold (`Kills for Unlock`), the item becomes unlocked.

- **Unlock from Discards**  
  - Discarding items from your own inventory or the post-battle loot screen also adds progress toward unlocking them.
  - Discard progress is scaled so that discarding a certain number of items equals the same unlock as killing that many enemies with them.

- **Own-culture bonuses**  
  - When you unlock items from other cultures, your clan culture can gain bonus progress toward its own gear.
  - This is done by picking random items of your clan culture (by tier) and adding extra progress to them in the background.

### Doctrines That Influence Availability

- **Clanic Traditions**  
  - Enables crafted weapons to appear in the list (one latest variant per design).
  - Also unlocks a **"Show Crafted" toggle** in weapon slots: when active, the list shows **only** crafted weapons.

- **Ancestral Heritage**  
  - Items of your clan or kingdom culture can be treated as available even if they aren't unlocked by kills/discards yet.

- **Pragmatic Scavengers / Battlefield Tithes / Lion's Share**  
  - Modify how kills contribute to unlock progress (see below).

### Unlocks From Kills (Played Battles)

At the end of a victorious battle you personally played, the mod:

1. Scans equipped items on each defeated enemy.
2. For each valid item, increments its unlock counter.

Doctrine effects:

- **Pragmatic Scavengers**  
  - Ally casualties can also contribute unlock progress (their fallen gear is considered "scavenged").

- **Battlefield Tithes**  
  - Kills by allied troops (not just yours) contribute unlock progress.

- **Lion's Share**  
  - Kills you personally land count double toward unlock progress.

When one or more items reach the unlock threshold:

- A popup summary appears listing some of the newly unlocked items.

### Unlocks From Discards (Inventory)

When you discard items from your party inventory:

- Each stack contributes progress toward unlocking that item.
- Higher amounts discarded = more progress.
- This is a separate source of progress from kills and uses a configurable ratio (`Discards for Unlock` vs `Kills for Unlock`).

A small popup may also appear if discarding pushes items over the unlock threshold.

### Special Unlock: Vassal Reward Secrets (Rulers)

Some cultures have special vassal reward items that rulers grant when you join them as a vassal.

There is an extra way to unlock these:

1. Capture a kingdom ruler after a battle (they must be the actual ruler of their kingdom).
2. You must have won the battle without allied armies leading alongside you.
3. In the post-battle captured lord conversation, a new option appears:  
   > "Reveal the secrets of your most precious artifacts and I will let you go."

Choosing this option:

- Applies the same relations penalty as taking them prisoner.
- Immediately releases the ruler after that.
- Automatically unlocks all vassal reward items for that ruler's culture that you haven't unlocked yet.
- Shows a popup listing the unlocked artifacts.

---

## Locked vs Disabled Items in the List

An item row may be disabled for several reasons:

- **Unlocking (x%)**  
  - The item is progressing toward unlock via kills/discards but hasn't reached the threshold yet.

- **Not in town**  
  - With **Restrict to Town Inventory**, the current settlement doesn't sell this item.

- **Skill requirement**  
  - Your troop's relevant skill is below the item's difficulty.

- **Tier too high**  
  - Item tier exceeds your **Allowed Tier Difference** (MCM).  
  - The **Ironclad** doctrine relaxes this for armor.

- **Not civilian**  
  - You're editing a **civilian set**, but the item is not flagged as civilian in the game.

Hovering a row shows a tooltip with item stats and requirements.

---

## Sorting & Filtering

Use the list header to sort by:

- **Name**
- **Category**
- **Tier**
- **Cost**

Each column can be toggled between ascending/descending order.

A **search box** filters items by:

- Name
- Category
- Type
- Culture

For performance reasons, the list caps at 1000 rows:

- If more items exist, only the top 1000 (post-sort) are shown.
- Your currently equipped and staged items are always forced into the list even if this would push it beyond 1000.

---

## Horses & Upgrade Requirements

- Unequipping a horse automatically unequips the harness, refunding any extra copies as needed.
- Upgrade requirements look at the best horse category across all sets:
  - Battle and civilian sets are considered, except when:
    - **Ignore Civilian Horse for Upgrade Requirements** (MCM) is enabled: civilian horses are ignored.
    - **Never require noble horse** (MCM) is enabled: noble horses are treated as warhorses for requirements.

For vanilla troops, you can optionally keep their original upgrade requirements; for custom troops, horse requirements are entirely driven by what you actually equip in their sets.
