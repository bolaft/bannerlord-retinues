---
title: Uninstalling
nav_order: 13
---

# Uninstall

Removing Retinues safely requires one in-game step first. This prevents broken saves caused by leftover references to custom troops.

---

## Why the "Purge" step is necessary

Retinues adds custom troop definitions to your save. Other parties, garrisons, and rosters may reference them. If you uninstall without cleanup, the game can try to load troops that no longer exist and crash.

The **Purge Custom Troop Data** button in MCM replaces all custom troops with safe culture counterparts and clears related state so your save no longer depends on the mod.

---

## Safe uninstall

1. **Load your save** (the one you want to keep playing).
2. Open **MCM → Retinues → Debug** and click **Purge Custom Troop Data**.  
   - Confirm the warning. Wait until you see the success message.
3. **Save** your game under a **new slot** (e.g., "pre-uninstall") and **exit to desktop**.
4. **Remove the mod files** according to your install method:
   - **Nexus (zip/manual):**
     - Delete the folder:  
       `<Bannerlord>\Modules\Retinues`
   - **Steam Workshop:**
     - In Steam, **Unsubscribe** from *Retinues*.
     - Optional: ensure the folder is gone:  
       `C:\Program Files (x86)\Steam\steamapps\workshop\content\261550\3592533567`

You can now continue that save without the mod.

---

## Emergency fallback: Retinues.Disabled (Nexus only)

On the Nexus Mods download page there is an additional **optional file**: `Retinues - Disabled`.

Use this **only as a last resort** if something goes badly wrong and Retinues itself prevents you from loading your save (for example, after a failed update) and you cannot wait for a proper fix.

How to use it:

1. In the launcher, **disable** `Retinues`.
2. **Enable** the `Retinues - Disabled` module instead.
3. Launch the game and try to **load your broken save**.

**Important limitations:**

- With `Retinues.Disabled`, the game will spawn **stub** troops in place of custom ones (you may see strange units like babies, peasants with odd gear, etc.).
- You will **lose all progress** related to Retinues systems: feats, XP, unlocks, and similar Retinues-specific data.
- This is still better than having to start a brand-new campaign if Retinues completely blocks your save.
- `Retinues - Disabled` is only available on **Nexus Mods** and does **not** exist on Steam Workshop.

---

## If you already uninstalled and the save crashes

If you removed Retinues without purging first and the save now crashes:

1. Reinstall/Resubscribe **Retinues**.
2. Load the affected save, perform the **Purge** (MCM → Retinues → Debug).
3. Save, exit, and uninstall again using the steps above.

If you are on Nexus and the game refuses to load even with Retinues reinstalled, you can try the **Retinues.Disabled** fallback described in the previous section.

---

## Notes

- Purge is **per-save**. Repeat for any other saves you want to keep.
- The purge operation is **irreversible**—consider making a manual backup first:
  - Saves: `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Game Saves`
