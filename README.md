# Retinues

A Mount & Blade II: Bannerlord mod that adds troop customization features to the game.

# TODO

## Features

- tech tree screen in bottom right
- maybe: select a doctrine to activate, and when active must do 3 tasks to unlock it? popup on task completion
- tech tree
    - item unlocks from ally kills
    - item unlocks from ally casualties
    - player kills count twice for unlocks
    - refund xp for retraining
    - 10% rebate on items of the troop's culture
    - 10% rebate on items of the clan's culture
    - 10% rebate on items of the kingdom's culture
    - +5% retinue cap
    - skill bonuses
- change troop culture
- tutorial on first game
- tool tip / communicate on skill caps/limits

## Refactor

- persist unlock kills & troop xp in persistence

## Fixes

- fix hints not appearing on disabled buttons

## Tests

- test persistence of item kills for unlocks

## Doctrines

- UNLOCKS
    - Lion's Share - Hero kills count twice for unlocks
        -> personally defeat 25 enemies in one battle
        -> personally defeat 5 tier 5+ troops in one battle
        -> personally defeat an enemy lord in battle
    - Battlefield Tithes - Can unlock items from allied party kills
        -> complete a quest for an allied lord
        -> save an allied lord from certain defeat
        -> turn the tide of a battle involving an allied army
    - Pragmatic Scavengers - Can unlock items from allied party casualties
        -> win a defensive battle in which allies suffer >50% casualties
        -> while in an army, win a battle in which allies suffer >50% casualties
        -> rescue a defeated allied lord from captivity
    - Cultural Heritage - Unlocks all items of clan & kingdom cultures
        -> win a 150+ battle while outnumbered and fielding only custom troops of your own culture
        -> win a tournament in a town of your own culture
        -> capture a fief of your own culture from an enemy kingdom
- EQUIPMENT
    - Cultural Pride - 10% rebate on items of the troop's culture
        -> win a tournament wearing only items of your own culture
        -> have a troop type wearing a full set of items of their culture get 100 kills in battle
        -> defeat a monarch of a different culture in battle
    - Clanic Traditions - 10% rebate on items of the clan's culture
        -> recruit 100 custom clan troops
        -> win a battle in which your companions get 50+ kills
        -> have a companion win a battle against 100+ enemies as a party leader
    - Royal Patronage - 10% rebate on items of the kingdom's culture
        -> recruit 100 custom kingdom troops
        -> have a companion of the same culture as your kingdom govern a kingdom fief for 30 days
        -> get 1000 kills with custom kingdom troops
    - Ironclad - No tier restriction for arms & armor
        -> equip a full set of tier 5+ items on a custom troop and have them get 100 kills in battle
        -> have 10 custom troops reach 80 in athletics skill
        -> win a 100+ battle, while outnumbered, and fielding only custom troops of tier 1, 2 or 3
- TROOPS
    - Iron Discipline - +5% skill cap
        -> upgrade 100 basic troops to max tier
        -> defeat an bandit leader in single combat
        -> 
    - Steadfast Soldiers - +5% skill points
        -> max out the skills of 10 custom troops
        -> 
        -> 
    - Masters-At-Arms - +1 upgrade branch for elite troops
        -> upgrade 100 elite troops to max tier
        -> defeat an enemy lord using only elite troops
        -> 
    - Adaptive Training - XP refunds for retraining
        -> have at least one troop reach level 200 in each skill 
        -> 
        -> 
- RETINUES
    - Indomitable - +25% retinue health
        -> have your retinues defeat 100 enemy troops of equivalent tier without a single casualty
        -> join a siege as a defender with a full-strength retinue and win
        -> have a retinue-only party win 3 defensive battles in a row
    - Bound by Honor - +20% retinue morale
        -> refuse payment for mercenary work three times.
        -> maintain a retinue-only party's morale above 80 for 30 days
        -> have one of your retinue win a tournament
    - Vanguard - +15% retinue cap
        -> clear a hideout using only your retinue
        -> win a 100+ battle using only your retinue
        -> have a retinue get the first melee kill in a siege assault
    - Immortals - +25% retinue survival chance
        -> have 100 retinue troops survive being struck down in battle
        -> win a 200+ battle without a single death on your side
        -> have a single retinue troop kill two tier 5+ units in one battle

## Script

Welcome to Retinues, a mod for Bannerlord that adds several troop customization features and mechanics to the game.

You can use the mod from an existing save, but I recommend starting a new game to get the intended experience.

After installing the mod, if you go in the clan screen you will notice a new tab, the "Troops" tab.

This is were most of the mod's features are located.

Initially, you will only have access to two retinues units, a basic one and an elite one.

They are low tier and poorly equipped, but soon enough you will be able to unlock better gear and improve their skills.

Retinues are a special kind of unit that are hired from regular troops of the same tier and culture.

They are limited in numbers and cannot be garrisoned.

Here in the equipment screen, you can see all the items that are available. At first, only a handful of basic items are unlocked.

So how do you unlock more items? Well it's simple: by doing battle.

As you defeat enemy troops, you will progressively unlock the items they were wearing.

Fight vlandians, you will get vlandian gear. Fight sturgians, you will get sturgian gear.

Of course to get the best stuff you will have to fight their best troops.

Once you have a better roster of items, you can go back to the equipment screen and outfit your retinues with better gear.

Notice that when you acquire an item, the previous one becomes stocked. So if you want to pass it on to a different troop, it will be free.

As your troops fight, they gain individual experience, but that experience also goes into a shared XP pool.

By spending from that pool you can improve your custom troops' skills.

In the special case of retinues, once they have reached their maximum amount of skill points, they can rank up and go up a tier.

Of course once they rank up, they will need to be converted from higher tier units.

Eventually your clan will acquire a fief or two, and you will now have access to the entire clan troop tree.

As you can see, at first that troop tree is based on your clan's culture.

But as you invest in their skills and equipment they will become more and more effective.

You can also rework the troop tree to add or remove upgrade paths as you see fit.

Unlike retinues those troops are recruited the normal way, but exclusively in your clan's fiefs.

AI lords can also recruit those troops from your clan's towns and villages: unlike retinues clan troops are not exclusive to the player.

If you want to improve your personal army even further, there is also the doctrines screen.

It's optional, and it presents itself as a tech tree of sorts.

Each doctrine has three feat requirements and a gold cost. Upon achieving the feats, you can unlock the doctrine by paying the gold cost.

Each doctrine provides a passive bonus to your custom troops.

Retinues have doctrines of their own, making them special troops, with special bonuses.

Finally, in the late game you might want to start your own kingdom.

When you do, you will unlock access to your kingdom's custom troop trees.

Kingdom troops are based on the kingdom culture, which can be different from your own clan's culture.

You will also be able to recruit king's guard retinues, doubling the amount of retinue troops you can have in your party.

Of course you'll have to equip and train them first, but at this point in the game you probably have the resources to do so.

Most of the mod's features are optional, and can be toggled on or off.

So if you want freeform troop customization for free with all items unlocked from the start, you can.

If you use Mod Configuration Menu, just make sure to download the MCM support module as well.

Otherwise you can always configure the mod from the config.ini file located in the Retinues.Core folder.

Retinues should be compatible with most other mods that introduce custom cultures and troops, including total overhaul mods.

Here for example I'm using Retinues with Realm of Thrones, and making a clan of Dothraki that adopted some Westerosi-style arms and armor.

That's it for now, I hope you enjoy the mod and have fun customizing your troops!