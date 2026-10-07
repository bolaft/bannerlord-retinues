# What's New in Retinues

This is a major update that rebuilds Retinues from the ground up. Everything the mod did
before is still here, but the whole thing has been rewritten, every screen redesigned, and
a large amount of new content and options added on top. This page covers what changes for
you if you are coming from the current release.

## Your save carries over

You do not need to start a new campaign. The first time you load an existing save, the mod
upgrades it to the new format and brings your progress across:

- Your custom troops carry over intact: names, level, gender, species, culture, skills,
  equipment sets (including civilian flags), body sliders, captains, formation and mariner
  flags, and the full upgrade tree.
- Your gear unlocks carry over, including partial progress on items you had not finished
  unlocking, and your equipment stockpiles are preserved.
- Doctrines you had unlocked are re-granted, and partial doctrine and feat progress is
  carried forward (rescaled to the new goals).
- Each troop's battle record (wins and losses, field and siege counts, kills and
  casualties) carries over, and each troop's unspent training experience is converted into
  the equivalent skill points.

A one-time notice appears on the world map the first time you load an old save, and your
save file is automatically backed up before the mod activates on a new version (you are
told the backup's name). A few things to be aware of:

- Settings are now stored differently (see below), so your options reset to their defaults
  once. Re-apply your preferences after updating.
- Any training or equipment changes still in progress at the time of the update are
  cancelled, so re-queue anything that was mid-way. Finished results are kept.
- The old renown-funded retinue auto-join no longer exists, so that reserve does not carry
  over.
- Migrating an old save is best-effort. It is more robust than before (a single bad troop
  no longer aborts the rest), but starting a fresh campaign is still recommended for the
  cleanest experience.

## Fewer mods to install

Retinues no longer needs Mod Configuration Menu (MCM) or ButterLib. In the old version
those were how you changed the mod's settings; now the settings live on their own page
inside the editor and save to a simple file, so you can remove both from your load order.
The only prerequisites left are Harmony and UIExtenderEx (now version 2.13.2 or newer).

## The editor is its own screen

In the old version the editor was a tab tucked inside the Clan Management screen. It is now
a dedicated full screen of its own, with a top tab bar (Editor, Doctrines, Library,
Settings) for switching views. You open it from:

- a new Troops button on the campaign map bar, next to Party, Clan, and Kingdom,
- the R hotkey on the map (it used to be Shift+R, and it is now on by default),
- the escape menu (the button is now called "Universal Editor"),
- and "open in editor" buttons in the encyclopedia, which now appear on clan and kingdom
  pages too, not just hero and troop pages.

You close it with Escape, like any other screen, instead of having to leave the clan
screen. The map-bar button and hotkey stay greyed out until your clan or kingdom actually
has a custom troop to edit.

## Build troops for other clans in your kingdom

As a ruler, you can now select any vassal clan in your kingdom and give it its own custom
troop trees, either copied from your kingdom or clan or built from scratch, and remove them
again later (reverting that clan to its default troops). Any clan or kingdom can now own
and keep its own editable troop tree, not just ones whose troops already differ from their
culture.

The old separate editing modes (one for your own troops, one for cultures, one for heroes)
have been merged into a single Universal Editor that handles any faction's troops and hero
looks in one place. It no longer needs a restart to switch on, can be toggled off in
settings, and when off it quietly falls back to your own troops instead of hiding the
button. There are also optional "Enforce Skill Limits" and "Enforce Equipment Limits"
toggles (off by default) if you want normal rules applied while editing other factions.

When a troop tree is first created, you choose how much of it to generate with a new
Starter Troops option (just the roots, lean trees, or full trees), how its starting gear is
chosen with a Starter Equipment option (random, copy one set, copy all sets, or empty), and
the starting skill level with a Starter Skills option. You can also set when clan and
kingdom troops unlock (for example always, with your first fief, or on becoming a ruler).

## Redesigned interface

Every editor screen has been redesigned around one consistent layout (model preview, list,
and detail panel):

- The Doctrines screen is reworked. Instead of all doctrines shown as side-by-side columns
  of cards that each open a popup, you browse a categorized list and selecting a doctrine
  shows its description, progress, feats, and Acquire button in a detail panel.
- The troop screen is reorganized into an Editor tab holding the Character and Equipment
  views.
- The Library and Settings pages (below) are entirely new.

## The Library

The Library is a new tab for managing the troop and faction builds you export. The old
version only had bare "Export All" and "Import All" file-picker buttons; now you get a full
in-game manager:

- Export a single troop or a single faction to its own file, instead of one all-in-one
  bundle. Exports are saved under Documents\Retinues\Exports.
- Browse all your saved exports (split into Factions and Troops) with a readable name,
  source faction, and contents preview, sortable by name or date.
- Import, re-export, rename, edit (opens the file in your default editor), or delete any
  saved export from inside the game, with confirmations. Importing replaces just the one
  troop or faction you pick.
- Convert a saved export into a standalone Bannerlord mod directly from the Library, written
  into your Modules folder (or Documents\Retinues\GeneratedMods if that folder is
  protected). This works for faction exports and vanilla-troop edits.

Importing now warns that imported characters bypass the mod's rules and may be considered
cheating in an ongoing campaign. Note that the export format was completely rewritten, so
export files from older versions of the mod cannot be imported and will need to be
recreated.

## More appearance options

- Mixed-gender units are new. Instead of a troop being all-male or all-female, it can now
  spawn as a blend of both, with the share controlled by a new Mixed Gender Ratio setting.
- The gender control is now a four-way cycle (male, female, male and mixed, female and
  mixed) rather than a plain toggle, with a small overlay icon showing the mix.
- The species picker (renamed from "Change Race" to "Change Species") now greys out species
  that will not work for a troop's culture or gender and explains why, instead of letting
  you pick anything. Switching species also rebuilds the troop's body, face, and hair to
  match.
- The old per-equipment-set "Gender Override" toggle is gone, replaced by the per-troop
  mixed-gender option above.

## Skills and training, reworked

- Troops now earn whole skill points over time and you spend one point to raise a skill by
  one, instead of spending a raw experience balance whose cost climbed with the skill's
  level. A single "Skill Points Must Be Earned" toggle (with a Gain Rate slider) replaces
  the old experience-cost sliders.
- Skill experience now follows the game's own rules, so battles, auto-resolved fights, and
  daily training all count automatically. Higher-tier troops take proportionally more
  experience per point.
- The shared skill-point pool now pools the points your troops earn (it used to pool raw
  editor experience).
- Training that takes time is now fully passive: staged skill points apply gradually on
  their own, with a configurable rate, and a new "Train While Travelling" option (on by
  default) lets them progress while you move instead of only while resting in a town. The
  old "go to a fief and start a timed training menu" step is gone.
- Lowering a skill always refunds its point now, and a troop's skills can no longer be
  dropped below what it had as a base unit before upgrading (with a one-time heads-up that
  retraining takes time).
- Your troops now show a notification when they earn a skill point.
- Various balance tweaks: the hero skill cap is now fixed at 360 (the configurable Hero
  Skill Cap setting was removed), mid-tier skill budgets were nudged up slightly, tier 2
  troops can train each skill to 60, and the top-tier captain and doctrine skill bonuses now
  scale with your settings instead of being flat amounts.

## How retinues work

- Retinues no longer trickle into your party on their own as you earn renown (the old
  auto-join behavior and its renown setting are gone). Instead you recruit a retinue right
  on the normal party screen by upgrading its matching troop, which shows up as an extra
  upgrade option. This replaces the old in-editor conversion that charged gold and
  influence.
- Retinues from cultures other than your own clan's must now be earned per culture by
  building up progress (winning tournaments and battles alongside allies, completing quests,
  owning fiefs and workshops of that culture). Your own culture's retinue is still available
  from the start, and a new Retinue Unlock Speed setting tunes the pace.
- Retinue capacity is now a single combined cap (around 15% of your party size, replacing
  the separate elite and basic caps), and the party screen greys out retinue upgrades with a
  "max retinue cap reached" note when you are full.
- Retinues get a default health bonus (+20, adjustable and toggleable), and the retinue
  skill cap and total bonuses can now be toggled individually.
- Ranking a retinue up now requires its skills to be fully maxed first, and costs skill
  points plus a fixed gold fee.

## AI lords get retinues too

- Other clans now field their own retinues. Tier 3 and higher clans gain household guard
  troops (and ruling clans a King's or Queen's Guard) and slowly promote regulars into them
  over time. This is on by default and tunable in settings (minimum clan tier, party cap,
  daily chance, leaders-only).
- When you lose or retreat from a battle against a clan that has retinues, their retinues
  can scavenge your fallen custom troops and permanently keep a higher-tier piece of your
  gear, with a post-battle popup naming what was taken.
- Enemy retinues also slowly improve their own equipment over the course of a campaign, so
  late-game enemy retinues are better kitted out.

## Special troops

- Custom militia, caravan, and villager troops now also show up in some related quest
  parties, and your custom militia is used when a settlement's garrison is rebuilt after a
  rebellion.
- Road Wardens now unlocks a third caravan troop, the Armed Trader, alongside the Caravan
  Master and Caravan Guard (on Bannerlord 1.2 and newer).
- Captain spawn frequency is now adjustable in settings (it used to be fixed at one per
  fifteen troops), and the Captains doctrine's objectives were reworked.
- The doctrine descriptions for these troops were rewritten to be clearer.
- The separate "no doctrine requirements" shortcut for special troops was removed; they now
  unlock by completing their doctrine, or all at once via the global feat-requirements
  toggle.

## Equipment

- You can now equip troops with items of any tier. The old limit on how far an item's tier
  could exceed the troop's tier is gone (weight and value budgets and mount tier
  requirements still apply).
- New optional per-tier weight and value budgets for a troop's battle gear (off by default),
  enforced when equipping and shown with color-coded warnings in the editor. Both are part
  of the Realistic preset.
- Mount restrictions are now three separate minimum-tier settings (regular mounts, war
  horses, noble horses) instead of a single "no mounts for tier 1" toggle.
- Staged equipment changes now also progress while travelling, via a new "Equip While
  Travelling" option (on by default), instead of only while waiting in a settlement.
- Showing crafted weapons in the equipment list no longer requires the Clan Traditions
  doctrine; the toggle is available to anyone.
- Base-game (non-DLC) players now have a single combined Siege toggle for equipment sets
  rather than separate siege-defense and siege-assault assignments.

## Equipment unlocks

- Workshops you own now passively unlock gear over time, each working on an item suited to
  its trade and its settlement's culture, with a popup announcing what it started on.
- New games now begin with a pool of pre-unlocked gear (a few items per slot up to a low
  tier, from your culture), configurable in settings.
- Gear unlocked from battle now also appears on the post-battle scoreboard, and creating new
  troops tells you what gear got unlocked in the process.
- Unlock progress now only comes from gear you actually fight against or discard; it no
  longer drifts toward random items of your own culture, and the "all culture equipment
  unlocked" toggle was replaced by earning it through the Ancestral Heritage doctrine.
- Unlock notifications are now a style choice (popup or log message), and pop-ups from
  different sources are combined into a single tidy summary. The master switch is now
  "Equipment Needs Unlocking" (on by default; turn it off to make everything instantly
  available).

## Recruitment

- Where your clan and kingdom troops can be recruited is now set with clear dropdowns (for
  example everywhere, your faction's fiefs, or nowhere), replacing the old toggles.
- Mixing custom troops with vanilla volunteers is now a simple on/off "Mix With Vanilla
  Troops" toggle, replacing the old proportion sliders.
- Who can recruit your custom troops is now a single "Allowed Recruiters" choice (everyone,
  your faction only, or just you), and same-culture-only is a single toggle.

## Formations and battle records

- Custom troops now always fight in the formation class you assign them, applied per troop.
  Formation overrides no longer need a setting to be turned on, and no longer require a troop
  to make up most of a formation to take effect.
- Each troop's battle record (kills, casualties, wins and losses, battle types, most-slain
  and most-feared) is tracked per troop and shown in a Statistics view in the editor. Naval
  battles are now tracked.

## Console commands

For players who use the developer console, there are new commands (for example creating a
retinue from any culture on demand, force-unlocking your clan or kingdom troops, granting
skill points, and readying a doctrine for purchase). Several older commands were renamed or
reorganized (for instance the doctrine and feat listings are now separate commands), so if
you used console commands before, check the current names.

## Everything else is still here

Clan and kingdom troop trees, retinues, militias, caravan and villager troops, captains,
doctrines and feats, equipment sets and stocks, appearance and hero customization, and
compatibility with mods like The Old Realms, Shokuho, and Bandit Militias all carry over.
This update is about making all of it work better, and adding to it, not replacing it.
