---
title: Troubleshooting
nav_order: 11
---

# Troubleshooting

> This page covers **Retinues V1**. The V2 beta uses **Troops → Settings** and has different configuration options.

Many problems can be solved by toggling options in the **Mod Configuration Menu (MCM)** or by using a few **cheats** to repair state.

> If you still need help after trying these, please see [Bug Reports](bugs.md) to submit a report.

---

## Common Symptoms (Q & A)

**Q: I can't see modded items in the list**  
**A:** If the modded items are not worn by any troop then you'll have to either unlock them by discards, unlock all items from the config menu, or use the cheat console (see below) to unlock items individually.

**Q: How can I equip my troops with smithed weapons?**  
**A:** You have to unlock the *Clan Traditions* doctrine perk first.

**Q: Why do some of my troops spawn naked?**  
**A:** Check that every enabled battle equipment set contains the intended gear. If troops were created, converted, or imported on Bannerlord 1.4+ before the v1.4.14.32 fix, update Retinues and **reapply their equipment in the editor or import them again**. Updating alone does not repair those existing loadouts. If alternate sets still cause problems, try **Force Main Battle Set In Combat** in MCM.

**Q: I should have unlocked this feat but it's not progressing.**  
**A:** You may have a mod conflict; you can unlock the feat using the cheat console (see below).

**Q: I want to uninstall Retinues but now my save crashes.**  
**A:** Follow the steps in the [Uninstalling](uninstall.md) section.

**Q: My save won't load at all / the game crashes before the save finishes loading.**  
**A:** If the game cannot reach the campaign map with Retinues enabled, see the **"Retinues - Disabled"** emergency fallback section in [Uninstalling](uninstall.md). It explains how to temporarily load your save using the `Retinues - Disabled` module so you can recover the campaign even if Retinues itself prevents loading.

**Q: The version on Steam Workshop updated automatically and now my save crashes.**  
**A:** You can go back to any previous version by downloading it from the Nexus Mods [download page](https://www.nexusmods.com/mountandblade2bannerlord/mods/8847?tab=files), in the "Old Files" section (unsubscribe from the Steam version first).

**Q: The game freezes on the battle deployment screen.**  
**A:** If you use **Adonnay's Troop Changer**, the freeze can be caused by load order. Make sure **Adonnay's Troop Changer** is placed *above* **Retinues** in the mod load order. If the problem persists, try disabling one of the two mods temporarily to confirm the conflict.

---

## Quick Fixes

1. **Check required mod versions**

   Use **Bannerlord 1.4.8 or the 1.5.4 beta** with the current V1 release. For Bannerlord 1.2.12, use the separate older build. Check that the required libraries meet these minimum versions:

   - **Harmony**  
     - **2.4.2 or later** for the current **Bannerlord 1.4 / 1.5 beta** build
     - or **2.3.6 or later** for the older **Bannerlord 1.2.12** build
   - **ButterLib** – **2.10.2 or later**
   - **UIExtenderEx** – **2.13.2 or later**
   - **Mod Configuration Menu v5** – **5.11.3 or later**

   Check these in your launcher (Mods list → click each dependency).

   - Update dependencies below these minimums and choose releases compatible with your Bannerlord version. A higher version number alone is not a reason to downgrade.
   - For **Steam Workshop users**:
     - Unsubscribe from **Retinues** and all its dependencies (Harmony, ButterLib, UIExtenderEx, Mod Configuration Menu v5).
     - Then subscribe again from the **Retinues** Workshop page and accept the *"Additional Required Items"* popup so Steam pulls the correct dependency versions.

2. **Mod order**

   - Put **Retinues** **higher (earlier)** in the **load order** than large overhauls that alter troops/equipment/UI.
   - If another mod also edits the Clan screen / troop systems, test with **only Retinues + dependencies**.
   - If you use **Adonnay's Troop Changer**, place it **above Retinues** in the load order to avoid freezes on the battle deployment screen.

3. **Try the "Default" preset**

   - In MCM → **Presets**, apply **Default**. If still blocked, try **Freeform** to remove restrictions for testing.

4. **Disable progression gates temporarily**

   - Turn **Off** in MCM:
     - **Unlock From Kills**
     - **Unlock From Discarded Items**
     - **Restrict Items To Town Inventory**
     - **Enable Doctrines** (or set **Disable Feat Requirements = On**)
   - If this fixes it, re-enable options one by one to find the cause.

5. **Clear Retinues MCM cache**

   - Close the game.
   - Delete the `Retinues` folder in  
     `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Configs\ModSettings\`  
     (for example: `C:\Users\YourName\Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Retinues`).
   - Restart the game and re-apply your options in MCM. This is especially useful after updating Retinues if configs seem "stuck" or behave strangely.

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
- For conflicts, selectively disable Retinues features to isolate.

---

## Still stuck?

- See [Bug Reports](bugs.md) to open a ticket.
