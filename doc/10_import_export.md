# 10) Import & Export

This guide explains how to **export** all your custom troop roots to a single XML file and how to **import** them back into another save (or after a reset).

> Exports/Imports deal with **root troop definitions** (your custom Regular/Elite roots, retinues roots, and their trees).

---

## Where files go

- **Folder:** `Modules/Retinues/Exports/`
- **Default names:**
  - From **MCM Export button:** `troops_YYYY_MM_DD_HH_mm.xml` (timestamped suggestion)
  - From **Console command:** `custom_troops.xml` if you don’t provide a name

You can safely create your own file names; the mod will add `.xml` if missing.

---

## Method 1 — Using Mod Options (MCM)

Open **Options → Mod Options → Retinues**.

### Export

1. At the top (**Import & Export** section), set **File name** if you want (optional).  
2. Click **Export Troops to XML**.  
3. A message will print the absolute path used. Your file is now in `Modules/Retinues/Exports/`.

### Import

1. Click **Import from XML**.  
2. Pick a file from the list (the *Confirm* button reads **Import**).  
3. The mod will:  
   - Make an **automatic safety backup** first (e.g., `backup_troops_YYYY_MM_DD_HH_mm.xml`)  
   - Then import and rebuild the selected roots  
4. A message will report how many **root troop definitions** were imported.

> **Important:** MCM export/import actions require you to be **in a running campaign** (not from the main menu).

---

## Method 2 — Using the Console

> You’ll need to have enabled the cheat console. Commands live under the `retinues` namespace.

### Export command

```text
retinues.export_custom_troops [fileName]
```
- If `fileName` is omitted, the mod uses `custom_troops.xml`.
- Prints the full path upon success.

### Import command

```text
retinues.import_custom_troops <fileName>
```
- Looks in `Modules/Retinues/Exports/`.
- If you pass a name without `.xml`, the mod also tries `<name>.xml` automatically.
- Reports how many **root** definitions were rebuilt.

---

## What gets exported

- **All currently defined custom troop roots** (regular & elite lines, retinues roots, etc.) with the data needed to **rebuild their trees** on import.
