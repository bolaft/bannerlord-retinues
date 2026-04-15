# Retinues Player Manual

## Overview

Retinues adds a suite of systems for building, training, and equipping your own personal forces throughout a campaign. At its core is the retinue: a household troop tied to your clan that follows you exclusively and can be developed into an elite unit over time.

Beyond these special units, Retinues also lets you build and maintain full custom troop trees for your clan and kingdom. These troops appear as volunteers and fill your faction's armies across the world. A doctrine system provides a progression layer, unlocking new troop types, equipment perks, and other bonuses as you accomplish gameplay objectives.

All of this is managed through the editor, opened from the Troops button on the campaign map or by pressing [R] (the hotkey can be toggled in settings). It covers every aspect of your forces: skills, equipment, appearance, and the troop tree. The Universal Editor extends the same tools to any faction in the game, and the Library lets you manage exported builds and convert them into standalone mods.

---

## Retinues

Retinues are special household guard troops tied to your clan. Unlike regular faction troops, they are personal to the player: they cannot be garrisoned or transferred to another party, and they stay with you at all times.

They start at a low tier but can be trained, equipped, and ranked up over the course of a campaign. With enough investment they can become some of the strongest troops on the battlefield.

### Starting Retinue

One retinue is created automatically when you start a new campaign or when you load an existing save for the first time. It is named after your clan and matches your clan's culture. If you are already a ruler at that point, a King's Guard or Queen's Guard is also created for your kingdom.

### Unlocking More Retinues

You can unlock one additional retinue per culture. Your own clan's culture is unlocked from the start. For every other culture, you accumulate unlock progress through gameplay activities tied to that culture:

- Winning a tournament: +50 points (for the tournament settlement's culture)
- Completing a quest successfully: +25 points (for the quest giver's culture)
- Owning a fief: +5 points per day per fief (for each fief's culture)
- Owning a workshop: +2 points per day per workshop (for the workshop settlement's culture)
- Winning a battle alongside allied parties: +10 points per allied party (for each ally's culture)

Each culture requires 1000 points to unlock. The overall speed of this progress can be adjusted with the Retinue Unlock Speed setting. When a culture is unlocked, a new retinue is created automatically and a popup appears to let you jump directly to the editor.

### Party Limits

The number of retinues you can field is capped at a ratio of your party size limit. The default is 15%, adjustable in settings. Doctrine bonuses can raise this cap further.

### Limitations

Retinues cannot be placed in garrisons or transferred to companion parties. They follow you exclusively and are intended to remain at your side throughout the campaign.

### Recruitment

Retinues do not appear in settlement volunteer lists. Instead, they surface as upgrade options on existing troops in your party.

When you open the party screen, each retinue is matched to the best fitting troop at one tier below it. The retinue then appears as an additional upgrade target on that troop, alongside any normal upgrade branches. Upgrading a troop into a retinue this way does not consume the retinue: it converts one unit of the source troop into the retinue type.

The source troop is chosen by scoring troops from your faction's custom tree first (if one has been unlocked), then falling back to your culture's troop tree if no custom match is found. The match is based on tier and formation category. Because this lookup runs dynamically every time the party screen opens, the source troop updates automatically if you modify your custom tree.

### Buffs

Retinues receive a set of passive bonuses on top of their normal troop stats. By default these are:

- +20 hit points
- +5 to all skill caps
- +20 to total skill points

Each of these bonuses can be toggled and adjusted individually in settings. The Realistic preset disables all three.

### Ranking Up

When a retinue has its skills fully maxed out, it becomes eligible to rank up to the next tier. Ranking up costs both gold and skill points:

- Gold cost: (current tier + 1) x 1000 denars. For example, ranking a tier 2 retinue to tier 3 costs 3000 gold.
- Skill point cost: 5 x current tier. For example, the same rank-up costs 10 skill points.

The effect is a tier increase, which raises the retinue's level by 5 and unlocks higher skill caps and totals.

The rank up action is available in the editor when all conditions are met. The editor will show what is blocking the action if requirements are not yet satisfied.

### AI Clan Retinues

Non-player clans can also have their own retinues. This feature is enabled by default and applies to clans that meet a minimum tier threshold (tier 3 by default). Each day there is a chance (25% by default) that an eligible party promotes one matching-tier troop into a retinue, up to a cap of 10% of the party's size by default. Kingdom rulers get a King's Guard or Queen's Guard in addition to their clan retinues. By default only the clan leader's party builds retinues; the Only Clan Leaders Have Retinues setting can be turned off to allow all lord parties in a clan to have their own.

AI retinues improve over time as their clan earns experience. If you lose a battle against a clan that has retinues, their retinues can also acquire equipment from your casualties, gradually upgrading their gear.

---

## Clan & Kingdom Troops

Beyond your retinues, you can build and customize full troop trees for your clan and your kingdom. These troops appear in volunteer pools, garrisons, and parties across the world and are edited from the same Troops screen as retinues.

Each faction gets two root lines: a basic tree and an elite (noble) tree. From those roots you can branch upgrade paths up to a maximum of 4 upgrade targets per troop.

### Unlocking

#### Unlock Conditions

Clan and kingdom troops are unlocked separately, and the exact trigger is configurable in settings:

- Clan troops unlock when you acquire your first fief by default. They can also be set to unlock from the start, or disabled entirely.
- Kingdom troops unlock when you found a kingdom by default. They can also be disabled entirely.

When troops are unlocked a popup appears, and if you are in the editor at the time you can jump straight to the new tree.

#### Starter Troops

The initial state of the tree when it unlocks depends on the Starter Troops setting:

- Roots Only: only the two root troops (basic and elite) are created. The rest of the tree is left empty for you to build manually.
- Lean Trees (default): a streamlined version of your culture's tree is copied. For each tier the system picks one representative troop per formation type (infantry, ranged, cavalry, horse archer), skips redundant branches, and connects everything back in a clean linear path. Troop names are replaced with faction-based names like "Clan Recruit" and "Clan Noble Recruit" rather than the original culture names.
- Full Trees: the entire culture tree is cloned verbatim, including all branches and upgrade paths. Troop names have their culture prefix stripped and replaced with your clan or kingdom name, the same as the other modes.

#### Starter Equipment

The equipment those starter troops spawn with is controlled separately by the Starter Equipment setting:

- Random Set (default): each troop gets a randomly generated equipment set assembled from items appropriate for that troop's tier and culture. Items are picked up to the tier limit set by Random Item Max Tier (default: tier 4).
- Single Set: each troop is given exactly one copy of the first equipment set from its culture template, both battle and civilian. No randomization.
- All Sets: every equipment set defined on the culture template is copied over as-is. This is the most conservative option and gives you the most to work from.
- Empty Set: troops are created with no items equipped at all. Use this if you want to outfit everything from scratch.

### Recruitment

Where your custom troops appear as volunteers depends on the Clan Troops Availability and Kingdom Troops Availability settings. The defaults make clan troops available in clan fiefs and kingdom troops available in kingdom fiefs. Both can be opened up to any settlement or restricted further.

The Same Culture Only setting prevents custom troops from appearing in settlements of a different culture. The Allowed Recruiters setting controls whether AI lords can also recruit your custom troops or only you can.

By default custom troops replace standard culture volunteers entirely in eligible settlements. If you want them mixed in alongside standard troops you can enable Mix With Vanilla Troops in settings.

### Militias and Special Troops

Three doctrines from the Troops column unlock additional custom troop types for your faction:

- Stalwart Militia unlocks custom militia troops (melee, ranged, and their elite variants). These replace the default cultural militia in your faction's towns and castles on a daily cycle.
- Road Wardens unlocks custom caravan troops (Caravan Guard, Caravan Master, Armed Trader). These are used by your faction's caravans.
- Armed Peasantry unlocks custom villager troops. These are used by your faction's village parties.

All of these troops are editable from the Troops screen like any other custom troop.

### Captains

The Captains doctrine unlocks Captain variants for all your regular troops. A Captain is a stronger version of a base troop that spawns periodically in its place among your forces. The spawn rate is configurable through the Captain Spawn Rate setting (default: 1 per 15 regular troops).

Captains cannot have upgrade targets of their own and cannot be ranked up. Their equipment and appearance are edited separately in the editor, but they share equipment costs with their base troop: any item already worn by the base troop is free to equip on the Captain and vice versa.

### Upgrades and Removal

You can add an upgrade target to any non-hero custom troop that is not at max tier and has not yet reached the maximum of 4 upgrade targets. When you confirm the name, the new troop is created one tier above its parent and inherits skills from it.

To remove a troop from the tree you must first remove all of its upgrade targets. Root troops (those with no parent) cannot be removed. When a troop is removed any units of that type in the world are converted to their culture counterpart.

### Other Clans In Your Kingdom

When you are a ruler, the editor also lets you manage the custom troop trees of other clans within your kingdom. Click the left banner from the Troops screen to select a different clan from the list. If the selected clan has no custom troops yet, you are prompted to initialize them by copying the kingdom's existing tree or by starting from scratch with culture roots.

Vassal clan troops follow the same rules as your own clan troops: they recruit in the clan's fiefs, respect the Allowed Recruiters and Same Culture Only settings, and can be edited in the same ways. You can also delete a vassal clan's custom troops from the editor, which reverts them to using kingdom or player clan troops.

---

## Skills & Experience

Your own custom troops (clan troops, kingdom troops, and retinues) have a skill sheet you can edit from the Troops screen, subject to per-skill caps and a total skill budget for the troop's tier. The XP and skill points system described in this section applies only to these troops.

Any troop from any culture or clan can also be edited through the Universal Editor, but XP is never a consideration there. Skills can be freely adjusted regardless of battle history or skill points balance.

### XP Sources

Custom troops accumulate skill point XP from all the usual sources: played battles, autoresolved battles, daily party training, and any other activity that grants troop XP. When the Skill Points Must Be Earned setting is active (on by default), all of that XP feeds into the skill point progress counter for the troop type. The exact threshold per skill point depends on the troop's tier and upgrade cost; higher-tier troops require proportionally more XP. The overall rate is adjustable with the Skill Points Gain Rate setting.

By default, skill points are tracked separately per troop type and persist across saves.

#### No XP Required (Freeform)

When Skill Points Must Be Earned is disabled, skill points are not gated behind battle performance. Skills can be freely allocated in the editor at any time without earning anything first.

### Shared Skill Points Pool

When the Shared Skill Points Pool setting is enabled, all custom troops contribute their battle XP to a single campaign-wide pool instead of per-troop pools. Skill points that accumulate in the pool can then be spent on any custom troop, regardless of which troop earned the XP. This makes it easier to develop a few specialists without having to spread battles evenly across your roster.

The pool balance is shown in the skill panel header when browsing troops (replacing the per-troop point count). A notification appears in the message log whenever the pool gains one or more skill points.

Skill points spent from the shared pool follow the same caps and limits as per-troop points. You cannot circumvent skill caps or the total budget simply because the points came from the shared pool.

Switching the setting off mid-campaign does not refund or clear either the per-troop points or the shared pool; they are both retained and become active again if the setting is toggled back on.

The Shared Skill Points Pool setting requires Skill Points Must Be Earned to be active.

### Spending Skill Points

Each time you increase a skill in the editor, one skill point is consumed from the troop's balance. Decreasing a skill refunds the point immediately.

When training takes time, decreasing a skill that is already committed (not pending) prompts a confirmation, since the point will be reset to pending and the trained values will not be recovered instantly.

### Skill Caps and Totals

Two limits apply when editing skills from the Troops screen (and optionally in the Universal Editor):

- Skill cap: the maximum level any single skill can reach for a troop of that tier. Configured per tier in the Skill Caps section of settings. Defaults range from 20 at tier 0 to 360 at tier 7.
- Skill total: the total sum of all skills a troop of that tier may have. Configured per tier in the Skill Totals section of settings. Defaults range from 90 at tier 0 to 1600 at tier 7.

Retinues receive bonuses on top of both limits (see the Buffs section above). Max-tier Captains receive a 20% bonus to both their cap and total. The Iron Discipline doctrine adds +5 to the cap for all troops. The Steadfast Soldiers and Masters at Arms doctrines add +20 to the total for basic and elite lines respectively.

You cannot raise a skill above the cap, and you cannot raise any skill if the total budget is already exhausted. Both limits are enforced visible in the editor, which shows the remaining budget at all times.

### Training Takes Time

When Training Takes Time is enabled, raising a skill in the editor does not apply instantly. The increase is stored as pending and applied gradually over time at the rate set by Trained Points Per Day (default: 1 skill point per day). Pending increases are shown in a different color in the editor so you can see what is committed versus what is still being trained.

When multiple skills have pending increases, each day one point is picked at random and applied. A notification appears in the message log whenever a troop improves a skill.

Training progress continues while you are travelling by default. This can be disabled with the Train While Travelling setting, in which case progress only advances while the party is stationary in a settlement.

There is no XP or gold cost to the training process itself; time is the only resource consumed.

### Skill Floor from Upgrade Sources

A troop's skills cannot be lowered below the highest value held by any of its upgrade sources. For example, if a tier-2 source troop has 80 in One Handed, you cannot set the tier-3 upgrade's One Handed below 80. This ensures that upgrading a troop is always meaningful and never produces a weaker unit.

The editor will show which source troop is setting the floor when a decrease is blocked.

### Context Restrictions

The Editing Restriction setting can limit when troop skills and equipment can be changed:

- None (default for Freeform): editing is always allowed.
- In Settlement: you must be inside any settlement to make changes.
- In Fief: you must be inside a settlement owned by your clan or kingdom. Retinues are exempt from the fief requirement and can be edited in any settlement.

---

## Equipment

### Loadout Types & Sets

Each troop has two categories of equipment: battle sets and civilian sets. Battle sets are the ones troops wear in combat; civilian sets are worn at all other times. These two categories are kept entirely separate, and you switch between them using the button above the character model in the equipment screen.

Battle sets carry an additional per-set configuration: each one can be marked as applying to field battles, siege battles, and naval battles. A set marked for field battles will be used in open-field engagements; one marked for siege battles will be used when assaulting or defending fortifications; one marked for naval battles will be used in sea combat, which requires the corresponding DLC. A single set can cover any combination of these battle types. At least one battle set must remain enabled for each type at all times, so the game always has a valid loadout to draw from.

### Managing Sets

#### Creating Sets

To create a new set, use the add button in the set navigation controls. A popup will ask whether you want to create a copy of the current set or an empty one. Copying the current set is useful when you want to make a variation with only a few changes; an empty set lets you build from scratch.

#### Removing Sets

To delete the current set, use the remove button. A confirmation prompt will appear before deletion. You cannot delete a set if it is the only one remaining in its category. You also cannot delete a battle set if doing so would leave any battle type without a set that covers it.

When economy is active and a battle set is deleted, the items it contained are returned to your stock automatically, provided no other set in the same roster also requires them.

### Slots

Each equipment set has the following slots: four weapon slots, Head, Cape, Body, Gloves, Leg, Horse, and Horse Harness. Weapon slots accept all equippable weapons; armor slots accept the appropriate armor type for each position.

The Horse Harness slot is only active when a horse is equipped. If you equip a new horse and the current harness is not compatible with it, the harness is cleared automatically.

### Shared Items Across Sets

When a troop has multiple sets within the same roster, items are shared intelligently between them. If a piece of equipment is already present in another set belonging to the same troop, equipping it in a second set does not incur an additional stock cost or purchase. The roster is treated as a whole, and only the number of copies that exceed what the other sets already require will draw from your stocks.

This means that a troop with two sets wearing the same sword in both does not count as requiring two swords; it only requires one.

### Stocks (Own Supply)

Each item in the game has its own stock count, which represents how many copies your forces have stockpiled. When economy is active (which is the case under the default settings), equipping an item draws one copy from its stock. Unequipping an item returns it to stock.

Stock can accumulate over time through battles, discards, and other sources. The stock count is shown alongside each item in the equipment list. Items with no stock are not blocked from purchase but will require spending coin instead of drawing from stocks.

This system only applies in the Troops screen. In the Universal Editor, stocks play no role and all items are freely assignable.

### Costs, Rebates & Purchases

When the Equipment Costs Money option is active, equipping an item that is not already in stock and not covered by another set in the roster requires spending coin. The cost is based on the item's value, scaled by the Cost Multiplier setting.

Two doctrines can reduce equipment costs. Cultural Pride grants a 20% discount on items matching your clan's culture. Royal Patronage grants a 20% discount on items matching your kingdom's culture. Both discounts can apply to the same item if both conditions are met.

If an item is available in stock, one copy is consumed at no coin cost. If the item is already equipped in another set of the same troop, it is treated as available and also costs nothing. Only items that must be freshly purchased draw coin from your wallet.

Deleting a set while economy is active refunds the items it contained back to stock, to the extent that no other set in the roster already requires them.

This system only applies in the Troops screen. In the Universal Editor, equipment is free and unrestricted.

### Staging vs Instant Equip

By default, equipping an item takes effect immediately. If the Equipping Takes Time option is enabled, however, troops do not finish changing their gear the moment you confirm a slot change. Instead, the new item is placed into a staging queue and is gradually applied as in-game time passes.

Slots with a staged item waiting are displayed differently in the UI: the color of the slot entry changes to indicate that a change is pending, and the tooltip shows the item currently equipped rather than the one staged. You can unstage an item at any time to cancel the pending change.

Progress toward applying staged items accumulates hour by hour. If the Equip While Travelling setting is enabled, progress also advances while your party is on the move; otherwise it only advances while you are resting in a settlement. The Equip Time Multiplier setting controls how quickly staged items are applied.

Hero troops are not subject to staging; their equipment changes are always instant. Staging only affects non-hero troops in the Troops screen.

### Weight and Value Limits

Two optional settings, Limit By Weight and Limit By Value, impose a budget on the total weight and total value of a troop's battle equipment. Both are off by default.

When a limit is active, each troop tier has a corresponding budget and the editor shows the remaining capacity alongside the equipment list. An item that would push the total over the budget cannot be equipped. Each limit has an independent multiplier setting that scales all tier budgets up or down.

Two Armory doctrines raise these limits when they are active, allowing higher-tier loadouts on troops that would otherwise exceed them. Both doctrines are overridden when their corresponding limit setting is off.

### Item Availability & Unlocks

The unlock system is only active when the Equipment Needs Unlocking option is enabled. It applies exclusively to the Troops screen. In the Universal Editor, all items are available without restriction regardless of any unlock settings.

When unlocking is active, each item requires 1000 points of progress before it becomes equippable. Items begin locked and are hidden from the equipment list until they accumulate at least one point of progress. Once they have some progress but have not yet reached the threshold, they appear at the bottom of their section in the list, sorted by how close they are to completion, but they cannot yet be equipped. Reaching 1000 points makes an item fully available.

On a new game, a small number of items per slot are pre-unlocked automatically, up to a configurable tier ceiling. You can adjust how many items start unlocked and what tier they reach using the Pre-Unlocked Amount Per Slot and Pre-Unlocked Max Tier settings.

Crafted items, meaning weapons you have smithed yourself, are always available regardless of unlock status. They are shown or hidden using a dedicated toggle in the equipment controls, which only appears in the Troops screen.

When an item fully unlocks, a notification appears. Its style (popup or log message) is configurable with the Item Unlock Notification setting.

#### Doctrines That Influence Availability

The Ancestral Heritage doctrine causes all items belonging to your clan's culture to be treated as permanently unlocked. You do not need to earn progress for them through any other means.

The Spoils doctrines affect how quickly items unlock through combat. See the section on doctrines for the full list and their effects.

#### Unlocks From Kills

When Unlock Items Through Kills is enabled, defeating enemies in played battles generates progress toward unlocking the equipment they were wearing. Each kill against an enemy wearing a given item contributes a fixed amount of progress based on the Required Kills To Unlock setting. By default, 100 kills against troops wearing an item fully unlocks it.

Progress is only gained in battles that are played out manually. Battles where your party loses generate no progress at all.

Several doctrines modify how kills are counted. The Lions Share doctrine doubles the progress earned from kills made by your own troops. The Battlefield Tithes doctrine extends unlock progress to kills made by allied troops in the same battle. The Pragmatic Scavengers doctrine counts allied casualties rather than kills, providing progress from troops lost on the battlefield.

#### Unlocks From Discards

When Unlock Items Through Discards is enabled, discarding items from your inventory contributes progress toward unlocking them. Each discard of a given item adds progress according to the Required Discards To Unlock setting. By default, discarding an item 10 times fully unlocks it.

This source can be useful for items you find frequently in loot but have not yet encountered in combat.

#### Unlocks From Workshops

When Unlock Items Through Workshops is enabled, each workshop you own works to unlock one item at a time. The workshop selects an item based on its type and the culture of its host settlement, and spends the configured number of days unlocking it. Once that item is fully unlocked, the workshop picks a new target automatically.

The Required Days To Unlock setting controls how long each workshop takes. With the default setting of 14 days, a single workshop unlocks roughly two items each month. Owning multiple workshops accelerates the overall pace since each one works independently.

Not every workshop type qualifies for this process; the selection depends on what the workshop produces and whether a matching item exists.

#### Special Unlock: Vassal Reward Secrets

There is one additional way to unlock a curated set of items tied to another kingdom's culture. If you capture the ruler of an enemy kingdom in battle while fighting solo, a dialogue option becomes available during the post-battle conversation. Choosing to demand their military secrets causes them to reveal the knowledge behind their most valued equipment, instantly unlocking all items in that culture's vassal reward category. The ruler is then released.

This can only be done by fighting the ruler down yourself in the battle, without assistance from other units.

### Locked vs Items With Partial Progress

Items that have never been seen and have no progress at all are hidden from the equipment list entirely, keeping the list from filling with hundreds of entries you have no information about.

Items that have acquired at least some progress but are not yet fully unlocked are shown at the bottom of their section, after all items that are ready to equip. They are sorted by progress percentage so that items close to completion appear first. These items cannot be equipped until the threshold is reached.

Once an item reaches full progress it moves up to the equippable portion of the list and can be selected normally.

### Sorting & Filtering

The equipment list supports both text filtering and column sorting. Typing in the filter field narrows the list to items whose name, category, type, or tier contains the entered text.

The sort buttons above the list allow you to sort by name, culture, tier, or value. Clicking a sort button once sorts ascending; clicking it again sorts descending. Clicking a third time clears that sort and returns to the default grouping order.

By default, weapon slots group items by weapon type, and armor slots group by item category. The horse slot groups by horse category as well. When a non-weapon slot contains a very large number of items, category grouping is applied automatically to keep the list manageable.

### Horses & Harness Compatibility

Not every harness fits every horse. Harnesses and horses each carry compatibility data, and equipping a harness that is not compatible with the currently equipped horse is not permitted. When you equip a new horse, any harness that is incompatible with it is removed from the slot automatically.

The Horse Harness slot remains locked and non-interactive until a horse is equipped. As soon as a horse is assigned, the harness slot becomes available and shows only items compatible with that horse.

### Mount Tier Restrictions

Three settings control which troops are permitted to use mounts at all, based on tier:

- Minimum Tier For Mounts: troops below this tier cannot be mounted. Default: tier 2.
- Minimum Tier For War Mounts: troops below this tier cannot use war horses. Default: tier 3.
- Minimum Tier For Noble Mounts: troops below this tier cannot use noble horses. Default: tier 4.

Setting any of these to 0 removes the restriction for that category.

---

## Appearance Customization

### Name & Culture

Every custom troop has a name you can change at any time. Clicking the rename button opens a text prompt where you can enter a new name. Empty names are not accepted.

Hero characters have a second name field for their surname or title. This is edited separately with its own button. Leaving the field blank removes the surname entirely.

A troop's culture affects its body properties, unlock discounts (through the Ancestral Heritage doctrine), and recruitment pool placement. To change a troop's culture, click the culture button and select the target culture from the list. The body properties are re-applied from that culture's template when you confirm, but the troop's current age is preserved. Retinues cannot have their culture changed.

When mods that add multiple species are present, a species selector also becomes available for non-hero troops. It shows only the species that are compatible with the troop's current culture and gender. Incompatible entries are shown greyed out with an explanation.

### Gender

Troops and heroes have a gender toggle button that switches the character between male and female. When alternate species are loaded the toggle additionally checks that a valid model exists for the target gender before allowing the change.

There is also a mixed gender option. When enabled, a portion of the units that spawn from this troop type will use the opposite gender's appearance. The percentage is set globally via the Mixed Gender Ratio setting. The mixed gender toggle appears on the character page for all non-hero troops.

### Appearance Controls (Troops)

Non-hero troops have a collapsible appearance panel that can be expanded with the customization button below the character model. It is available on both the character page and the equipment page.

The panel provides four adjustable traits: Age, Height, Weight, and Build. Each is controlled by a pair of backward and forward buttons that step through a fixed list of presets. These values represent ranges rather than exact numbers, so each preset covers a band and the actual appearance of individual spawned units will vary within that band.

Age has four presets, from young adult to elderly. Height, Weight, and Build each have six presets, from the lowest extreme to the highest. Units at the lowest or highest preset have the corresponding navigation button disabled.

Changing the culture of a troop re-applies the culture's default body properties, which may reset any preset adjustments you have made. The age setting is preserved across culture changes.

### Hero Character Editor

For hero characters the customization button opens the in-game character editor rather than the inline preset panel. This is the same editor used during character creation, giving access to the full range of facial features and appearance sliders. When the editor is closed, the character model in the editor refreshes to reflect the changes.

---

## Feats & Doctrines

### How It Works

Doctrines are permanent upgrades for your factions that you acquire one at a time from the Doctrines page of the editor. Each doctrine belongs to a category, and within each category they form a chain: to unlock a doctrine you must first acquire the one before it. The first doctrine in every chain has no prerequisite.

Before a doctrine can be acquired it must reach full progress. Progress is earned by completing feats tied to that doctrine. Each doctrine has three feats, and completing them fills the doctrine's progress bar. The doctrine can then be purchased for a coin and influence cost.

If the Enable Feat Requirements setting is turned off, doctrines skip the feat step and are immediately purchasable as soon as their prerequisite is acquired.

Some doctrines are marked as overridden when the setting they depend on is disabled. An overridden doctrine is skipped in the chain for prerequisite purposes: the next doctrine in the category treats the one before the overridden one as its prerequisite. An overridden doctrine cannot be acquired, and its feats do not appear.

### Doctrine Categories

There are five doctrine categories. Each contains four doctrines forming a chain from tier 1 to tier 4. Costs rise with each tier.

#### Spoils Doctrines (Unlocks)

This category covers how your forces learn from battle and accumulate knowledge of enemy equipment. The doctrines in this chain build on each other to expand which sources of battlefield activity contribute to item unlocks. Earlier doctrines in the chain improve the efficiency of kills made by your own forces; later ones extend coverage to allies and to casualties taken rather than inflicted. The final doctrine in the chain causes all items belonging to your clan's own culture to be unlocked from the start, bypassing the normal unlock process for that culture entirely.

Doctrines in this category that rely on kill-based unlocking are automatically overridden when that system is disabled in settings.

#### Armory Doctrines (Equipment)

This category covers the economics and limits of your army's equipment. The first two doctrines in the chain reduce costs when outfitting troops with items from your clan's or kingdom's culture. The later doctrines raise the permitted limits on equipment weight and value, allowing higher-tier loadouts to be used. Each of these doctrines is overridden when its corresponding limit or cost system is disabled in settings.

#### Roster Doctrines (Forces)

This category unlocks additional custom troop types for your faction. Each doctrine in the chain opens a new troop category: custom militia for your towns and castles, custom guards for your caravans, custom villagers for your village parties, and finally Captain variants for all your regular troops. These troop types are editable through the same Troops screen as the rest of your roster once unlocked.

#### Training Doctrines (Troops)

This category improves the skill development of your troops. The first doctrine raises the skill cap for all your custom troops. The next two extend the total skill budget for your basic and elite lines separately. The final doctrine increases the rate at which your troops earn skill points from activity. That last doctrine is overridden when skill points do not need to be earned.repeatabl

#### Retinues Doctrines

This category strengthens your retinues specifically. The four doctrines in this chain increase retinue health, raise their morale, expand how many you can field, and increase the chance they survive after a battle loss. All four are overridden if the retinue system itself is disabled.

### Feats

Each doctrine has three feats. Feats are objectives that you accomplish through gameplay, and completing them fills the parent doctrine's progress bar. A doctrine becomes purchasable once its bar reaches 100.

Each feat has a target to reach and a worth value. The worth determines how much of the doctrine's 100-point progress the feat contributes when completed. A feat worth 40 contributes 40 points; completing all three feats of a doctrine therefore always sums to exactly 100.

Some feats are one-time challenges that complete when their target is first met. Others are repeatable: they reset after each completion and can be completed again, each time adding their worth to the parent doctrine's progress. Repeatable feats are primarily found in categories tied to continuous campaign activities like purchasing equipment or winning battles over time.

Feats are only active while their parent doctrine is in progress, meaning its prerequisite has been acquired but it has not yet been acquired itself. Feats are not tracked for doctrines that are locked, overridden, or already acquired.

When a feat is completed a notification appears, either as a message in the log or as a popup, depending on the Feat Complete Notification setting.

### Settings

The entire doctrine system can be disabled with the Enable Doctrines setting. When disabled, the Doctrines page is hidden from the editor.

Enable Feat Requirements controls whether feats must be completed before a doctrine can be acquired. Turning it off makes every unlocked doctrine immediately purchasable without any feat progress.

Doctrines Cost Money and Doctrines Cost Influence independently control whether acquiring a doctrine deducts coin and influence respectively. Both are on by default. The associated multiplier settings scale the cost amounts up or down.

The default costs scale with tier position within the category. The first doctrine in a chain is the cheapest; the fourth is the most expensive. Multipliers affect all tiers proportionally.

---

## Universal Editor

The Universal Editor lets you view and modify any troop tree or hero in the game, not just your own faction's. None of the economy systems apply: no XP, no costs, no stocks, no unlock checks. Everything is freely editable.

It is enabled by default and can be disabled in settings.

### Access

Open it from the Universal Editor button in the escape menu. Encyclopedia pages for heroes, clans, kingdoms, and individual units also carry a shortcut button that opens the editor directly on that subject.

### Culture Editor

Click the left banner to switch to any culture in the game. The troop list shows that culture's full tree with no editing restrictions.

Two optional settings, Enforce Skill Limits and Enforce Equipment Limits, apply the same caps used in your own Troops editor here. Both are off by default.

### Heroes & Clan Troops

Click the right banner to select a clan. The character list switches to show that clan's heroes and retinues instead of a culture tree, where they can be edited the same way as any other troop. Minor factions troops are also editable from the clan editor.

### Reset to Default

When you have modified vanilla troops in the current selection, a Reset action becomes available. You can revert either the entire current culture or a single troop. Only non-hero vanilla troops that have been changed are eligible.

The reversion is scheduled on confirm and takes effect after you save and restart.

---

## Import & Export

All of this is handled from the Library tab in the editor. Export files are XML stored in `Documents\Retinues\Exports\`, which the Library reads automatically.

### Exporting

Use the Export button in the editor's top panel. A popup asks whether to export the entire current faction or a single troop from it. Heroes cannot be exported. You then enter a filename; if a file with that name already exists you are prompted to overwrite.

### Library

The Library page shows all saved exports in two groups: Factions and Troops. Selecting an entry shows its name, type, source file, and for faction exports a summary of the troops it contains.

Four actions are available on each entry:

- Import: applies the export to the current game. For troop exports you choose which troop in the current faction to replace. For faction exports you choose which faction to override. The editor opens on the result after a successful import.
- Convert: generates a standalone Bannerlord module from the export and writes it to your Modules folder. If that folder is protected it falls back to `Documents\Retinues\GeneratedMods\`. Restart the game to load it.
- Edit: opens the raw XML file in your default text editor.
- Delete: removes the export file from disk.

---

## Cheat Console Commands

All commands live in the `retinues.` namespace. After activating cheat mode, open the console with `~` and type `retinues.` to see autocomplete suggestions for everything listed here.

### Skill Points

`retinues.add_skill_points <troop_id> <amount>` adds the given number of skill points to a troop. Use `retinues.list_custom_troops` first to get the correct troop ID for your clan and kingdom troops.

### Item Unlocks

`retinues.unlock_item <item_id>` unlocks a single item for the equipment screen. This has no effect when the unlock system is disabled.

### Doctrines & Feats

`retinues.doctrines_list` prints every doctrine with its ID, current state, and progress. `retinues.feats_list` prints every feat with its ID and progress.

`retinues.doctrine_unlock <doctrine_id>` sets a doctrine's feat progress to full, making it ready to purchase in the Doctrines page. You still need to spend the coin and influence cost to acquire it. `retinues.feat_complete <feat_id>` marks a single feat as completed, contributing its worth toward the parent doctrine's progress.

### Fixes

These commands bypass the normal unlock conditions and are useful when save state is inconsistent or an unlock notification was missed:

- `retinues.unlock_clan_troops` unlocks your clan troop tree without requiring a fief
- `retinues.unlock_kingdom_troops` unlocks your kingdom troop tree without requiring a kingdom
- `retinues.create_retinue <culture_id> <name>` creates a new retinue for your clan from the given culture, bypassing the unlock point requirement
- `retinues.list_custom_troops` lists all custom troop IDs for your clan and kingdom
