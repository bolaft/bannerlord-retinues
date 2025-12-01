---
title: Skills & Experience
nav_order: 4
---

# Skills & Experience

Troops in Retinues gain, spend, and (sometimes) refund XP. This XP is separate from vanilla XP and is used to improve custom troops (including retinues) through the editor.

You train skills directly from the **Troops** tab, using XP earned from battles and training.

---

## XP Sources

### 1) Played Battles (on mission end)

When you fight the battle yourself, your custom player-side troops gain XP per valid kill, scaled by the victim's tier.  
The XP is awarded after the battle ends.

### 2) Simulated / Autoresolved Battles

When a battle is simulated, your custom troops (including those present in allied parties) receive a smaller, proportional amount of XP based on:

- Enemy forces, and  
- Your side's troop counts snapshotted at battle start.

This keeps autoresolve competitive without eclipsing played battles.

### 3) Daily Training

Each in-game day, your main party's custom troops gain training XP derived from the vanilla training model, at a reduced fraction.

> Only custom troops managed by Retinues receive XP from these systems. Vanilla troops keep using vanilla rules.

---

## Improving Skills

Troop skills define combat aptitude and item requirements. In Retinues, you can train, cap, and manage skills directly from the **Troops** tab.

There are eight editable combat skills:

- **Athletics**
- **Riding**
- **One Handed**
- **Two Handed**
- **Polearm**
- **Bow**
- **Crossbow**
- **Throwing**

Hold **Shift** to increase or decrease by **5 points** at a time. When skills are free and instant, holding **Ctrl** will increase or decrease instantly by the maximum value.

---

## Caps, Totals, and Bonuses

Two limits gate training:

1. **Per-skill cap**  
   The maximum value a single skill can reach.

2. **Total points**  
   A pool of points spread across all eight skills.

Both scale with the troop's **tier**.

Retinues adds extra bonuses:

- **Retinue bonus**  
  Retinues get additional **cap** and **total points** compared to regular troops.

- **Doctrines**  
  - **Iron Discipline** → **+5 cap** to all skills.  
  - **Steadfast Soldiers** → **+10 total skill points**.

---

## XP Cost and Training Flow

Each +1 skill level costs XP roughly like:

> `XP cost = base + (per_point * current_value)`

When you increase a skill, Retinues either:

- **Applies immediately** (default mode), spending XP at once, or  
- **Stages** the point (if "Training Takes Time" is enabled in config).

If training takes time:

- The panel shows how many hours of training are required for the staged points.
- You complete the training via a town menu action (similar to staged equipment).

---

## Spending XP

### Skill Increases

Raising a skill consumes XP from the troop's XP pool (or from the shared pool, if configured).  
Higher skills cost more XP per point.

### Rank Up (Retinues)

Ranking up a retinue consumes:

- **Gold**, and  
- **XP** from the retinue's associated troop.

Both requirements must be met to rank up.  
See: [Retinues](./retinues.md).

---

## Refunding XP

- With the **Adaptive Training** doctrine:  
  Lowering a non-staged skill point refunds the XP that was spent on that point.

- Without Adaptive Training:  
  XP is **not refunded** when you decrease skills; only staged (unsaved) points can be removed for free.

---

## Per-Troop vs Shared XP Pool

In Mod Options (MCM) you can choose:

- **Per-troop XP**  
  Each custom troop has its own XP pool. Training that troop only uses its own XP.

- **Shared XP pool**  
  All custom troops draw from a single global XP pool.  
  Simpler to manage: "one bucket" of XP that you distribute as you wish.

Shared pools are easier to reason about; per-troop pools reward focusing on a smaller roster.

---

### Captains & XP

Captains are tightly coupled to their base troop:

- A **Captain** and its **base troop** always share the **same XP pool**.
- In **per-troop XP** mode:
  - Training the base troop or its Captain consumes XP from a **shared** pool for that pair.
- In **shared XP** mode:
  - This distinction is invisible in practice, since all troops already draw from the same global pool.

---

## When You Can / Can't Change a Skill

### Increment (+)

You **cannot** increase a skill if any of these are true:

- The skill is already at the **per-skill cap** (including bonuses).  
- You have reached the **total points** limit (including staged changes).  
- The troop (or shared pool) **doesn't have enough XP** for the next point.  
- The new value would **exceed a child upgrade's** skill level (children should not be "worse" than their parents).

### Decrement (–)

You **cannot** decrease a skill if:

- It would drop **below 0**.  
- It would drop **below item requirements** of currently equipped gear.  
- It would go **below the parent troop's** skill level.

When XP costs are enabled and you lower a non-staged point, XP is **not** refunded unless the **Adaptive Training** doctrine is active.

---

## Context Restrictions

Depending on your configuration:

- Editing skills may require being inside a settlement or specifically in one of your fiefs (clan/kingdom).
- If the context is not allowed, you'll get a popup explaining why the action is blocked.

These rules apply both to:

- Direct skill changes, and  
- Any action that implicitly changes skills (e.g. resetting, cloning with skill differences, etc.).
