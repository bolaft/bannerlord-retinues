---
title: Cheat Console
nav_order: 14
---

# Cheat Console Commands

Use these console commands to test builds, speed up progression, or recover from edge cases.  
They all live under the `retinues` namespace in the in-game cheat console.

---

## General syntax

```
retinues.<command> [arguments...]
```

If you pass a wrong number/type of arguments, the command prints a short **Usage** help line.

---

## Troop XP

- **Add XP to a troop**
  ```
  retinues.troop_xp_add <troopId> [amount]
  ```
  Adds `amount` XP (default **1000**) to the specified troop. Prints the troop name and amount added.

- **List active custom troops**
  ```
  retinues.list_custom_troops
  ```
  Lists **IDs, names, and current XP** for all active custom troops in your game.

> Use `retinues.list_custom_troops` to grab the correct `<troopId>` for `retinues.troop_xp_add`.

---

## Item unlocks

- **Unlock a specific item**
  ```
  retinues.unlock_item <itemId>
  ```
  Instantly unlocks the item so it can appear in the equipment editor (subject to your other rules).

- **Reset all unlocks**
  ```
  retinues.reset_unlocks
  ```
  Clears every item unlock so you can retest the progression from scratch.

---

## Item stocks

- **Set stock count for an item**
  ```
  retinues.set_stock <itemId> <count>
  ```
  Directly sets the tracked stock for an item (useful if you're playing with town-inventory restrictions or stock-sensitive rules).

---

## Doctrines & feats

Browse, advance, or complete feats that gate certain doctrine bonuses.

- **List all doctrines and feats (with progress)**
  ```
  retinues.feat_list
  ```
  Prints every doctrine, its feats, and each feat's current/target progress.

- **Advance a feat by amount (default 1)**
  ```
  retinues.feat_add <FeatNameOrType> [amount]
  ```
  Accepted identifiers typically include: full type name, short nested name (e.g. `MAA_1000EliteKills`), or a unique suffix.

- **Set a feat to an exact progress**
  ```
  retinues.feat_set <FeatNameOrType> <amount>
  ```
  Useful for precise testing of unlock thresholds.

- **Mark a single feat complete**
  ```
  retinues.feat_unlock <FeatNameOrType>
  ```
  Sets its progress to the target.

- **Mark all feats complete**
  ```
  retinues.feat_unlock_all
  ```
  Completes every feat in one go.

---

## Fixes

- **Fix main party**
  ```
  retinues.fix_main_party
  ```
  Attempts to fix some weird behaviors such as party size limit being stuck at 20, or the main party not having a troop number on the world map.

---