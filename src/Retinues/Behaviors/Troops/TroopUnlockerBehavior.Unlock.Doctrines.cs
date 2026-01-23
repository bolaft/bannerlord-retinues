using System;
using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Unlocks;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Base;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Troops
{
    public sealed partial class TroopUnlockerBehavior
    {
        /// <summary>
        /// Handles doctrine acquisition events to trigger troop unlocks.
        /// </summary>
        protected override void OnDoctrineAcquired(Doctrine doctrine)
        {
            // Just run the doctrine unlock pass; it will no-op if not relevant.
            TryUnlockNow(fromBootstrap: false);
        }

        /// <summary>
        /// Attempts to unlock doctrine-tied special troops for player clan and kingdom.
        /// Bootstrap runs do not show popups.
        /// </summary>
        private static void TryUnlockDoctrineTroopsForPlayerFactions(bool fromBootstrap)
        {
            if (!Configuration.EnableDoctrines)
                return;

            if (!HasAnyDoctrineTroopUnlocks())
                return;

            // Suppress CharacterCloner unlock popups when running from bootstrap.
            var unlockSink = !fromBootstrap ? new List<WItem>(128) : null;

            var changed = false;

            // Track what we created so we can show one popup with a representative troop.
            var created = new List<WCharacter>(16);

            var clan = Player.Clan;
            if (clan?.Base != null)
                changed |= TryUnlockDoctrineTroops(clan, unlockSink, created);

            var kingdom = Player.Kingdom;
            if (kingdom?.Base != null)
                changed |= TryUnlockDoctrineTroops(kingdom, unlockSink, created);

            if (!changed)
                return;

            if (unlockSink != null && unlockSink.Count > 0)
                ItemUnlockNotifier.ItemsUnlocked(
                    ItemUnlockNotifier.UnlockMethod.Troops,
                    unlockSink
                );

            // Popup only for event-driven unlocks (not bootstrap).
            if (fromBootstrap)
                return;

            // If we created nothing (edge case), don't show a popup.
            if (created.Count == 0)
                return;

            // Prefer showing a "higher value" representative.
            var select = PickDoctrinePopupTroop(created);
            if (select?.Base == null)
                return;

            var count = CountCreatedTroops(created);

            // Use the currently active faction context for opening the editor:
            // Prefer kingdom if it exists, else clan.
            var faction = (IBaseFaction)Player.Kingdom ?? Player.Clan;

            ShowUnlockPopup(faction, select, count);
        }

        /// <summary>
        /// Returns true if any doctrine that unlocks special troops is acquired.
        /// </summary>
        private static bool HasAnyDoctrineTroopUnlocks() =>
            DoctrineCatalog.StalwartMilitia.IsAcquired
            || DoctrineCatalog.ArmedPeasantry.IsAcquired
            || DoctrineCatalog.RoadWardens.IsAcquired;

        /// <summary>
        /// Performs doctrine-tied unlock checks for a single faction.
        /// </summary>
        private static bool TryUnlockDoctrineTroops<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            List<WItem> unlockSink,
            List<WCharacter> created
        )
            where TWrapper : BaseMapFaction<TWrapper, TFaction>
            where TFaction : MBObjectBase, IFaction
        {
            var changed = false;

            if (DoctrineCatalog.StalwartMilitia.IsAcquired)
                changed |= UnlockMilitiasIfNeeded(faction, unlockSink, created);

            if (DoctrineCatalog.ArmedPeasantry.IsAcquired)
                changed |= UnlockVillagerIfNeeded(faction, unlockSink, created);

            if (DoctrineCatalog.RoadWardens.IsAcquired)
                changed |= UnlockCaravansIfNeeded(faction, unlockSink, created);

            return changed;
        }

        /// <summary>
        /// Unlocks the villager troop for the given faction if not already unlocked.
        /// </summary>
        private static bool UnlockVillagerIfNeeded<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            List<WItem> unlockSink,
            List<WCharacter> created
        )
            where TWrapper : BaseMapFaction<TWrapper, TFaction>
            where TFaction : MBObjectBase, IFaction
        {
            if (faction?.MapFaction == null)
                return false;

            var culture = faction.Culture;
            if (culture?.Base == null)
                return false;

            var template = culture.Villager;
            if (template?.Base == null)
                return false;

            if (faction.Villager != null)
                return false;

            var troop = Cloner.CloneTroop(
                template,
                skills: true,
                equipments: true,
                intoStub: null,
                unlockItems: true,
                notifyUnlocks: false,
                unlockSink: unlockSink
            );

            if (troop?.Base == null)
                return false;

            troop.Name = BuildFactionTroopName(troop.Name, faction.Name, culture);
            troop.HiddenInEncyclopedia = false;

            faction.SetVillager(troop);
            created?.Add(troop);

            // Set basic root as upgrade target if present.
            if (faction.RootBasic != null)
                troop.AddUpgradeTarget(faction.RootBasic);

            return true;
        }

        /// <summary>
        /// Unlocks caravan troops for the given faction if not already unlocked.
        /// </summary>
        private static bool UnlockCaravansIfNeeded<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            List<WItem> unlockSink,
            List<WCharacter> created
        )
            where TWrapper : BaseMapFaction<TWrapper, TFaction>
            where TFaction : MBObjectBase, IFaction
        {
            if (faction?.MapFaction == null)
                return false;

            var culture = faction.Culture;
            if (culture?.Base == null)
                return false;

            var specs = new List<CloneSpec>(3)
            {
                new(faction.CaravanMaster, culture.CaravanMaster, faction.SetCaravanMaster),
                new(faction.CaravanGuard, culture.CaravanGuard, faction.SetCaravanGuard),
                new(faction.ArmedTrader, culture.ArmedTrader, faction.SetArmedTrader),
            };

            return CloneAndAssignMany(specs, unlockSink, created, faction.Name, culture);
        }

        /// <summary>
        /// Unlocks militia troops for the given faction if not already unlocked.
        /// </summary>
        private static bool UnlockMilitiasIfNeeded<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            List<WItem> unlockSink,
            List<WCharacter> created
        )
            where TWrapper : BaseMapFaction<TWrapper, TFaction>
            where TFaction : MBObjectBase, IFaction
        {
            if (faction?.MapFaction == null)
                return false;

            var culture = faction.Culture;
            if (culture?.Base == null)
                return false;

            var specs = new List<CloneSpec>(8)
            {
                // Core militia templates (both versions)
                new(
                    faction.MeleeMilitiaTroop,
                    culture.MeleeMilitiaTroop,
                    faction.SetMeleeMilitiaTroop
                ),
                new(
                    faction.MeleeEliteMilitiaTroop,
                    culture.MeleeEliteMilitiaTroop,
                    faction.SetMeleeEliteMilitiaTroop
                ),
                new(
                    faction.RangedMilitiaTroop,
                    culture.RangedMilitiaTroop,
                    faction.SetRangedMilitiaTroop
                ),
                new(
                    faction.RangedEliteMilitiaTroop,
                    culture.RangedEliteMilitiaTroop,
                    faction.SetRangedEliteMilitiaTroop
                ),
            };

            return CloneAndAssignMany(specs, unlockSink, created, faction.Name, culture);
        }

        /// <summary>
        /// Clone specification used to eliminate repetitive clone+set boilerplate.
        /// </summary>
        private readonly struct CloneSpec(
            WCharacter current,
            WCharacter template,
            Action<WCharacter> setter
        )
        {
            public readonly WCharacter Current = current;
            public readonly WCharacter Template = template;
            public readonly Action<WCharacter> Setter = setter;
        }

        /// <summary>
        /// Clones and assigns many troops from a (current, template, setter) table.
        /// </summary>
        private static bool CloneAndAssignMany(
            List<CloneSpec> specs,
            List<WItem> unlockSink,
            List<WCharacter> created,
            string factionName,
            WCulture culture
        )
        {
            var changed = false;

            for (int i = 0; i < specs.Count; i++)
            {
                var s = specs[i];
                changed |= CloneAndSetIfNeeded(
                    s.Current,
                    s.Template,
                    unlockSink,
                    s.Setter,
                    created,
                    factionName,
                    culture
                );
            }

            return changed;
        }

        /// <summary>
        /// Clones and sets a troop if the current is null and template is valid.
        /// </summary>
        private static bool CloneAndSetIfNeeded(
            WCharacter current,
            WCharacter template,
            List<WItem> unlockSink,
            Action<WCharacter> setter,
            List<WCharacter> created,
            string factionName,
            WCulture culture
        )
        {
            if (template?.Base == null)
                return false;

            if (current != null)
                return false;

            var troop = Cloner.CloneTroop(
                template,
                skills: true,
                equipments: true,
                intoStub: null,
                unlockItems: true,
                notifyUnlocks: false,
                unlockSink: unlockSink
            );

            if (troop?.Base == null)
                return false;

            troop.Name = BuildFactionTroopName(troop.Name, factionName, culture);
            troop.HiddenInEncyclopedia = false;

            setter(troop);
            created?.Add(troop);

            return true;
        }

        /// <summary>
        /// Picks a representative troop for the doctrine unlock popup.
        /// </summary>
        private static WCharacter PickDoctrinePopupTroop(List<WCharacter> created)
        {
            if (created == null || created.Count == 0)
                return null;

            // Priority: caravan master -> militia elite -> militia -> villager -> first
            for (int i = 0; i < created.Count; i++)
            {
                var c = created[i];
                if (c?.Base == null)
                    continue;

                var id = c.StringId ?? string.Empty;

                if (id.Contains("caravan") && id.Contains("master"))
                    return c;
            }

            for (int i = 0; i < created.Count; i++)
            {
                var c = created[i];
                if (c?.Base == null)
                    continue;

                // "Elite militia" tends to be more interesting than basic militia.
                if (
                    (c.Name ?? string.Empty).IndexOf("elite", StringComparison.OrdinalIgnoreCase)
                    >= 0
                )
                    return c;
            }

            return created[0];
        }

        /// <summary>
        /// Counts how many troop entries were created, expanding trees if present.
        /// </summary>
        private static int CountCreatedTroops(List<WCharacter> created)
        {
            if (created == null || created.Count == 0)
                return 0;

            var count = 0;

            for (int i = 0; i < created.Count; i++)
            {
                var c = created[i];
                if (c?.Base == null)
                    continue;

                // These are single troops (not trees), but keep it future-proof.
                if (c.Tree != null && c.Tree.Count > 0)
                    count += c.Tree.Count;
                else
                    count += 1;
            }

            return count;
        }
    }
}
