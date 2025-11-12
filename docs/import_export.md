---
title: Import & Export
nav_order: 10
---

# Import & Export

Retinues uses a unified XML format for moving troops between saves, which can include clan & kingdom troops (player troops) and culture troops (vanilla and modded troops used by existing factions).

You can export either or **both**, and you can import either or **both** from the same file.

> Exports/Imports operate on **root troop definitions and their trees**. Roster contents, XP, or equipment unlock progress are **not** exported.

---

## Where files go

- **Folder:** `Modules/Retinues/Exports/`
- **Filename:** you are prompted for a name (e.g. `troops_YYYY_MM_DD_HH_mm.xml`). The `.xml` extension is added automatically if missing.

---

## Exporting

There are two places to export from:

### From Mod Options (MCM)

1. Open **Options → Mod Options → Retinues**.
2. In **Import & Export**, click **Export Troops to XML**.
3. **Choose sections** to include (tick one or both).  
4. **Enter a filename** when prompted.  
5. A confirmation popup shows the absolute path of the saved file.

> You must be **in a running campaign** (not the main menu).

### From the Editor (Studio)

1. On the world map, open the **Troop Editor** from the escape menu.
2. Click **Export All**.
3. **Choose what to export** (tick one or both).  
4. **Enter a filename** when prompted.  
5. A confirmation popup shows where the file was written.

---

## Importing

Importing is symmetrical and always asks for confirmation before applying changes.

1. Click **Import from XML** (MCM or Editor).  
2. The file picker lists **only valid Retinues exports** (correct root).  
3. If the selected file contains both kinds of troops, select which one you want, or both. 
5. After applying, a **success popup** should appear.

> **Note:** Importing **replaces** your current root troop definitions for the selected sections.
> For **volunteers/recruits**, it can take **a couple of in‑game days** for settlements to refresh and reflect newly imported culture roots.

---

## Format details (for advanced users)

```xml
<RetinuesTroops>
  <Factions>
    <!-- Player Troops (custom Clan + Kingdom) -->
    <!-- Internal structure is versioned and handled by the mod. -->
  </Factions>

  <Cultures>
    <!-- Culture Troops (all culture roots) -->
    <!-- Internal structure is versioned and handled by the mod. -->
  </Cultures>
</RetinuesTroops>
```

Either `<Factions>` or `<Cultures>` can be **absent**; the importer only offers what's present in the file.
