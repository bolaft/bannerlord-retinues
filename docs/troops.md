---
title: Clan & Kingdom Troops
nav_order: 3
---

# Clan & Kingdom Troops

Besides the special [Retinues](./retinues.md), there are several kinds of faction-bound custom troops you can build:

- **Clan Troops** (tied to your **clan**)
- **Kingdom Troops** (tied to your **kingdom**)

Unlike Retinues (your personal household guard), faction troops are the broader troop trees and service troops that populate your rosters, parties, garrisons, volunteers, caravans, and villager parties.

Each faction layer (clan or kingdom) provides:

- A **Regular Tree** (non-noble line)  
- An **Elite Tree** (noble line)  
- Unlockable **Militias** aligned with your culture  
- Unlockable **Special Troops** (Caravan Guards, Caravan Masters, Villagers) when the right doctrine is active  

---

## Unlocking

- **Clan Troops** → unlock when you acquire your first fief.  
- **Kingdom Troops** → unlock when you found your own kingdom.

On unlock, you choose how to initialize that faction's troop trees:
- **Start from scratch:** minimal roots at low tier; build up over time.
- **Clone entire culture tree:** instantly copy the vanilla culture lines as a starting point.

> This choice is per faction (clan vs kingdom) and **not reversible** for that layer.

Militias and special service troops unlock via doctrines (see [Doctrines](./doctrines.md)):

- **Stalwart Militia** → unlocks custom militias.  
- **Road Wardens** → unlocks custom caravan guards and caravan masters.
- **Armed Peasantry** → unlocks custom villagers.

---

## Recruitment

Faction troops join your army the vanilla way (with helpful Retinues options):

- **Clan troops** are available in any settlement owned by your clan.
- **Kingdom troops** are available in any settlement owned by another clan of your kingdom.

> It might take an in-game day or two for volunteers to refresh after unlocking a custom troop tree.

- **Restrict To Owned Settlements:** disable in MCM to recruit your clan/kingdom custom troops from **any** settlement. Leave enabled for stricter, more "realistic" availability.

> Retinues-specific auto-hire and manual conversion do *not* apply here; those are for the [Retinues](./retinues.md) only.

---

## Militias & Special Troops

### Militias

- Unlock the **Stalwart Militia** doctrine to gain access to customizable militias.
- Custom militias replace the default cultural ones in your faction's fiefs automatically (daily swap).
- You can edit their equipment, skills, and name from the Troops editor, but:
  - They are still used as militia garrison troops in your settlements.
  - Their availability is tied to your faction's fiefs, not to normal volunteer lines.

### Caravan Guards, Caravan Masters & Villagers

- Unlock the **Road Wardens** and **Armed Peasantry** doctrines to gain access to custom Caravan Guards, Caravan Masters, and Villagers.
- When unlocked for a faction:
  - Retinues creates custom versions of:
    - **Caravan Guard**
    - **Caravan Master**
    - **Villager**
  - These custom troops replace the vanilla culture ones for:
    - Your faction's **caravans** (guards & masters).
    - Your faction's **villager parties**.
- You can edit them in the **Clan → Troops** editor like any other custom troop.

### Captains

- Unlock the **Captains** doctrine to gain access to **Captain** variants of regular troops.
- A Captain is a **stronger version** of a regular troop:
  - Always **+1 tier** compared to its base troop.
  - Shares the same role and upgrade branch as its base.
- When Captains are enabled:
  - **1 in 15** spawns of that troop will be the Captain variant instead of the base troop.
- Captains are edited in **Captain Mode** in the editor:
  - They have their own equipment and appearance.
  - They **share XP** with their base troop.
  - Equipping items already worn by the base troop is **free** for the Captain (and **vice versa**).

---

## Upgrades & Removal

### When can I add an upgrade?

You can add a new upgrade to a troop only if all of the following hold:

- **Not a militia, not a retinue.** Militias and Retinues cannot receive upgrade targets.
- **Not at max tier.** Max-tier troops can't branch further.
- **Upgrade slots remain.**  
  - **Basic** troops: up to **2** upgrades.  
  - **Elite** troops: **1** upgrade by default, **2** if the *Masters at Arms* doctrine is unlocked.
- **Context allows editing.** If your settings restrict editing to settlements/fiefs, you must be in the right place.

**What gets copied into the new upgrade?**  
When you confirm the new name, the game creates a child troop and copies skills from the parent (but *not* equipment, unless equipment is free and instant), and sets its tier to one above its parent.

---

### When can I remove a troop?

You can remove a troop only if:

- It's *not* a militia and *not* a retinue (regular faction troops only).
- It is deletable:
  - Root troops (troops with no parent) can't be removed.  
  - Troops that have upgrade targets can't be removed (remove their upgrades first).
- Context allows editing (same settlement/fief rules as above). The removal action checks this and blocks with a popup if disallowed. 

**What happens when I remove a troop?**

1. Any staged equipment/skill changes for that troop are cleared.
2. Its equipment is stocked for later use.  
3. Any existing units of that troop in your world are converted to their culture counterpart.

---

## Import / Export

- Custom troops can be exported and imported from one save to another via the mod config menu or the cheat console.
- This includes **clan troops, kingdom troops, militias, and special service troops** (Caravan Guards, Caravan Masters, Villagers), as long as they belong to your custom trees.

→ See: [Importing and Exporting Troops](./import_export.md)
