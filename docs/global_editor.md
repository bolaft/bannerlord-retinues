---
title: Global Editor
nav_order: 5
---

# Global Troop Editor

The **Global Troop Editor** (also called **Studio Mode**) lets you edit almost every troop in the game world, not just your own clan and kingdom troops.

Use it to:

- Rebuild **culture troop trees** (regular/elite lines).
- Customize **militias, caravan troops, villagers, and minor clan troops**.
- Adjust **civilians and bandits** for each culture.
- Edit **heroes** from other clans:
  - name
  - equipment
  - skills (including non-combat skills)
  - personality traits
  - appearance

> For your own clan and kingdom troops (and retinues), see the regular [Troops](./troops.md) and [Equipment](./equipment.md) editors.

---

## Access

You can open the Global Troop Editor in two ways:

1. **From the escape menu**
   - Press `Esc` in campaign.
   - Click the **Troop Editor** button.
   - This opens the editor directly in **Studio Mode**.

2. **From the Clan → Troops tab**
   - Open the **Clan** screen → **Troops** tab.
   - Click the **"Global Editor"** link in the top right.
   - This switches from the **personal editor** (your clan/kingdom) to the **global editor**.

To go back to your own troops:

- Click the **"Personal Editor"** link in the top right, or simply close the editor and reopen the Clan → Troops tab.

> The Global Troop Editor respects the **Enable Global Troop Editor** option in [Configuration](./configuration.md).  
> If that option is disabled (or auto-disabled by incompatibility), the Global Editor entry points are disabled.

---

## Modes: Culture vs Heroes

The global editor can work in two modes, controlled by the **top banner buttons**:

- **Culture Editor** – left banner
- **Heroes Editor** – right banner

### Culture Editor (left banner)

Click the **culture banner** (left side of the top panel) to choose a culture.

In Culture mode you can edit:

- **Regular troops** – basic and elite trees for that culture.
- **Militias** – melee/ranged militias and their elite variants.
- **Caravan troops** – **Caravan Guards** and **Caravan Masters**.
- **Villagers** – the culture's main villager troop.
- **Civilians** – shop workers, beggars, tavern staff, town/village civilians, etc.
- **Bandits** – the main bandit line for that culture.

The troop list is grouped as in the Troops tab:

- Retinues (if any)
- Elite / Regular
- Militia
- Caravans
- Villagers
- Civilians
- Bandits

The **Civilians** and **Bandits** groups only appear in **Culture** mode.

> Some cultures may be skipped if they are not fully initialized or have no valid root troops.

### Heroes Editor (right banner)

Click the **clan banner** (right side of the top panel) to pick a **non-player clan** belonging to the currently selected culture.

In Heroes mode you can edit:

- **Adult heroes** from the selected clan:
  - Name
  - Gender & body (for heroes this uses the full character editor; see [Appearance](./appearance.md))
  - Skills and attributes (including **non-combat skills**)
  - **Personality traits**
  - Equipment (battle & civilian sets)

By default, the main grid shows the vanilla skills that are most relevant to combat.  

- **DLC skills** and **modded skills** are also supported, click the **Show more skills** toggle to display them.

The troop list becomes a list of **heroes** instead of regular troops.  
In this mode:

- The **Import All**, **Export All**, and **Reset All** buttons are **disabled**.
- The editor treats heroes as live game entities: changes apply directly to the current save.

---

### Minor clan troops

Minor clan troops are edited through the same **culture → clan** flow used by the Heroes Editor:

1. Select the **culture** that the minor clan belongs to (left banner).
2. Select the **minor clan** (right banner / clan banner).

Once both are selected, the Global Editor exposes that clan's unique troop templates so you can adjust their equipment and progression alongside other global troops.

---

### Effects

Changes in Studio Mode affect:

- Any existing troops whose data is read from the edited templates.
- All **future spawns** of those troops.

---

### Global Reset

The **Reset All** button on the top bar schedules a **full reset of culture troops** back to vanilla defaults.

- Player clan/kingdom troops are **not** touched.
- The reset actually runs when you **save and reload** the game.

Use this when:

- You want to discard an old or broken culture setup.
- You imported a mismatched file and want to start fresh.

You'll see a confirmation popup explaining that the reset happens on next load.

---

## Encyclopedia shortcuts

For quick access, you can open the editor directly from a troop or hero's **encyclopedia page**:

1. Open the encyclopedia entry for the troop or hero.
2. Click the **Retinues editor button** at the **top-right of the character portrait**.

This jumps straight into the appropriate editor (personal or global) with that troop or hero pre-selected.
