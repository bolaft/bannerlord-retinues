---
title: Uninstalling
nav_order: 13
---

# Uninstall

The improved cleanup is included in **V1 1.4.14.33** and **V2 2.0.0.14**. If using an older release, update within your current branch before uninstalling. Switching from V2 to V1 is not an uninstall procedure.

Retinues troops can be referenced by parties, prisoners, village recruits, quests and other campaign records. Removing the module while those references remain can cause crashes on recruitment, daily updates or save loading.

## Safe uninstall

1. **Back up your campaign**, then load it with Retinues enabled.
2. Open the cleanup tool for your version:
   - **V1:** MCM → Retinues → Debug → Purge Custom Troop Data.
   - **V2:** Troops → Settings → Uninstall → Purge. Choose **Replace** to keep troop numbers, or **Remove** to remove custom troops from army and prisoner rosters. Recruitment slots and other campaign references are replaced in either mode.
3. Wait for the verified success message. **If cleanup is incomplete, reload your backup and report `Documents/Retinues/Logs/debug.log`. Do not remove Retinues or overwrite your backup.**
4. **Save to a new slot**, wait for saving to finish, then **exit to desktop**.
5. Remove Retinues through your mod manager or delete `<Bannerlord>/Modules/Retinues` for a manual install. Workshop users should unsubscribe from the installed item (`3599557394` for Bannerlord 1.4 / 1.5 beta; `3592533567` for the older 1.2.12 release). Check for duplicate manual/Workshop installations.
6. Load the new save, recruit in a village, let a day pass, then save and reload again. Keep the original backup until these checks succeed.

The cleanup replaces custom troops with similar non-Retinues troops, including troops from other installed mods. It checks the result and omits Retinues-specific saved data. If you reopen the prepared save with Retinues still enabled, troop generation stays disabled. To continue with your custom troops, return to your original backup.

Cleanup is **per campaign**. Repeat it for other saves you want to continue without Retinues. It cannot repair every save already damaged by a failed uninstall or guarantee compatibility with every other mod.

---

## Emergency fallback: Retinues - Disabled

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

1. Reinstall **the same Retinues branch** the campaign used, updated to a version with the improved cleanup. Do not downgrade a V2 campaign to V1.
2. Load your **pre-uninstall backup**, then use the cleanup tool for that branch.
3. Follow the new-save, exit and removal steps above. If you no longer have a backup, retain the affected save and report it with the log and load order; recovery is not guaranteed.

If you are on Nexus and the game refuses to load even with Retinues reinstalled, you can try the **Retinues.Disabled** fallback described in the previous section.

---

## Notes

- Purge is **per-save**. Repeat for any other saves you want to keep.
- The purge operation is **irreversible** - consider making a manual backup first:
  - Saves: `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Game Saves`
