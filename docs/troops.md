---
title: Clan & Kingdom Troops
nav_order: 3
---

# Clan & Kingdom Troops

There are two kinds of faction-bound troops you can build with Retinues:
- **Clan Troops** (tied to your **clan**)
- **Kingdom Troops** (tied to your **kingdom**)

Unlike Retinues* (your personal household guard), faction troops are the broader troop trees that populate your rosters, parties, garrisons, and volunteers.

Each faction layer (clan or kingdom) provides:

- A **Regular Tree** (T1 → …)  
- An **Elite Tree** (noble line)  
- Unlockable **Militias** aligned with your culture

---

## Unlocking

- **Clan Troops** → unlock when you **acquire your first fief**.  
- **Kingdom Troops** → unlock when you **found your own kingdom**.

> When the *Recruit Anywhere* option is enabled, clan troops are immediately unlocked even if you don't own a fief.

On unlock, you choose how to initialize that faction's troop trees:
- **Start from scratch:** minimal roots at low tier; build up over time.
- **Clone entire culture tree:** instantly copy the vanilla culture lines as a starting point.

> This choice is per faction (clan vs kingdom) and **not reversible** for that layer.

---

## Recruitment

Faction troops join your army the **vanilla way** (with helpful Retinues options):

- **Clan troops** are available in any settlement owned by your clan.
- **Kingdom troops** are available in any settlement owned by another clan of your kingdom

> It might take an in-game day or two for volunteers to refresh after unlocking a custom troop tree.

- **Recruit Anywhere (optional):** Enable in MCM to recruit your clan/kingdom custom troops from **any** settlement. Disable for stricter, more “realistic” availability.

> Retinues-specific auto-hire and manual conversion do *not* apply here; those are for the [Retinues](./retinues.md) only.

---

## Militas

- Unlock the **Cultural Pride** doctrine to gain access to customizable militias.
- Custom militias replace the default cultural ones in your faction's fiefs automatically (daily swap).

---
## Upgrades & Removal

### When can I add an upgrade?

You can add a new child to a troop only if all of the following hold:

- **Not a militia, not a retinue.** Militias and Retinues cannot receive upgrade targets.
- **Not at max tier.** Max-tier troops can't branch further.
- **Upgrade slots remain.**  
  - **Basic** troops: up to **2** children.  
  - **Elite** troops: **1** child by default, **2** if the *Masters at Arms* doctrine is unlocked.
- **Context allows editing.** If your settings restrict editing to settlements/fiefs, you must be in the right place.

**What gets copied into the new child?**  
When you confirm the new name, the game creates a child troop and copies skills from the parent (but *not* equipment nor existing upgrades), and set its tier to one above its parent.

---

### When can I remove a troop?

You can remove a troop only if:

- It's *not* a militia and *not* a retinue (regular faction troops only).
- It is **deletable**:
  - **Root troops** (troops with no parent) can't be removed.  
  - Troops that have **upgrade targets** can't be removed (remove their upgrades first).
- **Context allows editing** (same settlement/fief rules as above). The removal action checks this and blocks with a popup if disallowed. 

**What happens when I remove a troop?**

1. Any **staged equipment/skill changes** for that troop are cleared.
2. Its **equipment is stocked** for later use.  
3. Any **existing units** of that troop in your world are converted to their culture counterpart.

---

## Doctrines

**Doctrines** provide passive bonuses that can benefit your clan and kingdom troops and enable new customization options.  
→ See: [Doctrines](./doctrines.md)

---

## Import / Export

- Custom troops can be exported and export from one save to another via the mod config menu or the cheat console.
→ See: [Importing and Exporting Troops](./import_export.md)
