# Skills

Troop **skills** define combat aptitude and item requirements. In Retinues, you can train, cap, and manage skills directly from the **Troops** tab.

There are eight editable combat skills: Athletics, Riding, One Handed, Two Handed, Polearm, Bow, Crossbow and Throwing

Hold **Shift** to increase or decrease by 5 points at a time.

---

## Caps, totals, and bonuses

Two limits gate training:

1) **Per-skill cap:** max value a single skill can reach.  
2) **Total points:** a pool of points spread across all eight skills.

Both scale with **tier**. Retinues also adds bonuses:

- **Retinue bonus**: extra cap and total for retinue troops.  
- **Doctrines**:  
  - *Iron Discipline* → +5 **cap**.  
  - *Steadfast Soldiers* → +10 **total**.

---

## XP cost and training flow

- Each +1 costs XP: `base + (per_point * current_value)`
- When you click **+**, Retinues either:
  - Applies immediately (default mode), spending XP at once, or
  - **Stages** the point (if “training takes time” is enabled).

> See: [Experience](./experience.md)

If training takes time, the panel shows **Training required** hours if points are staged; finish training via the town menu action.

---

## When you can/can’t change a skill

### Increment (+)
You **can’t** increase a skill if any of these is true:

- It’s already at the **per-skill cap** for the troop’s tier/bonuses.  
- You’ve hit the **total points** limit (including staged changes).  
- The troop **doesn’t have enough XP** for the next point.  
- The new value would **exceed a child upgrade’s** skill level.

### Decrement (-)
You **can’t** decrease a skill if:

- It would go **below 0**.  
- It would drop **below item requirements** of currently equipped gear.  
- It would fall **below the parent troop’s** skill level.

When XP costs are enabled and you lower a non-staged point, **XP is not refunded** (unless the *Adaptive Training* doctrine is unlocked).

---

## Context Restrictions

Depending on your settings, editing may require being **in a settlement** or **in one of your fiefs** (clan/kingdom). If not allowed, you’ll get a popup explaining the reason.

