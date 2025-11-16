---
title: Troubleshooting
nav_order: 11
---

# Troubleshooting

Many problems can be solved by toggling options in the **Mod Configuration Menu (MCM)** or by using a few **cheats** to repair state.

> If you still need help after trying these, please see [Bug Reports](bugs.md) to submit a report.

---

## Common Symptoms (Q & A)

**Q: I can't see modded items in the list**  
**A:** If the modded items are not worn by any troop then you'll have to either unlock them by discards, unlock all items from the config menu, or use the cheat console (see below) to unlock items individually.

**Q: I have fiefs but the recruits are still vanilla troops?**  
**A:** It can take a couple of in‑game days for volunteers to refresh and be replaced by your custom troops.

**Q: How can I equip my troops with smithed weapons?**  
**A:** You have to unlock the *Clan Traditions* doctrine perk first.

**Q: Why do some of my troops spawn naked?**  
**A:** You probably enabled an alternate equipment set and left it empty. If it is not the case, try enabling the *Force Main Battle Set* mod option.

**Q: I should have unlocked this feat but it's not progressing.**  
**A:** You may have a mod conflict; you can unlock the feat using the cheat console (see below).

**Q: I want to uninstall Retinues but now my save crashes.**  
**A:** Follow the steps in the [Uninstall](uninstall.md) section.

**Q: The version on Steam Workshop updated automatically and now my save crashes.**  
**A:** You can go back to any previous version by downloading it from the Nexus Mods [download page](https://www.nexusmods.com/mountandblade2bannerlord/mods/8847?tab=files), in the "Old Files" section (unsubscribe from the Steam version first).

---

## Quick Fixes

1. **Mod order**
   - Put **Retinues** **higher (earlier)** in the **load order** than large overhauls that alter troops/equipment/UI.
   - If another mod also edits the Clan screen / troop systems, test with **only Retinues + dependencies**.

2. **Try the "Default" preset**
   - In MCM → **Presets**, apply **Default**. If still blocked, try **Freeform** to remove restrictions for testing.

3. **Disable progression gates temporarily**
   - Turn **Off** in MCM:
     - **Unlock From Kills**
     - **Unlock From Discarded Items**
     - **Restrict Items To Town Inventory**
     - **Enable Doctrines** (or set **Disable Feat Requirements = On**)
   - If this fixes it, re-enable options one by one to find the cause.

---

## DLLs blocked by Windows

Sometimes Windows marks downloaded `.dll` files as "blocked" for security reasons. When that happens, Bannerlord may:

- Crash on startup when loading mods.
- Fail to load Retinues at all (no MCM entry, features not working).
- Log `FileLoadException` / "operation is not supported" errors about Retinues or other mod assemblies.

You can fix this in two ways:

1. **Manual unblocking (Retinues only)**  
   - Close the game and Steam.
   - Browse to your game's Modules folder, then:
     - `...\Mount & Blade II Bannerlord\Modules\Retinues\bin\Win64_Shipping_Client\`
   - For each `.dll` in that folder (there should only be one: `Retinues.dll`):
     - Right-click → **Properties**.
     - On the **General** tab, if you see an **"Unblock"** checkbox, tick it.
     - Click **Apply** → **OK**.

2. **Unblock all mod DLLs at once**  
   If you have many mods, you can use the community "Unblock Dlls" utility, which automatically unblocks all `.dll` files under `Modules` in one go:  
   <https://www.nexusmods.com/mountandblade2bannerlord/mods/397>  
   - Download and follow the instructions on the Nexus page.
   - Run it once after installing or updating mods that were downloaded via a browser.

After unblocking, start the game again and check if Retinues appears correctly in MCM and the crash is gone.

---

## Repair & test with cheats

Use the in-game cheat console (namespace `retinues`). Common commands:

- **Items**
  - `retinues.unlock_item <itemId>` - unlock a specific item
  - `retinues.reset_unlocks` - reset item unlock progression
  - `retinues.set_stock <itemId> <count>` - force stock for town-restricted mode
- **Troops & XP**
  - `retinues.list_custom_troops` - list troop IDs
  - `retinues.troop_xp_add <troopId> [amount]` - add skill XP to the troop
- **Doctrines & Feats**
  - `retinues.feat_list`
  - `retinues.feat_unlock <FeatNameOrType>` / `retinues.feat_unlock_all`

See [Cheat Console](cheats.md) for full syntax and examples.

---

## Compatibility & mod order

- Place **Retinues** **before** large troop overhauls and UI packs.  
- Avoid multiple mods patching the **Clan Screen** UI simultaneously.  
- For conflicts, selectively disable Retinues features (**Unlock From Kills**, **Doctrines**) to isolate.

---

## Still stuck?

- See [Bug Reports](bugs.md) to open a ticket.
