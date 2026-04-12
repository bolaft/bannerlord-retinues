using System.Collections.Generic;
using Retinues.Behaviors.Unlocks;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Base;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Faction troop unlocking behavior.
    /// </summary>
    public sealed partial class TroopUnlockerBehavior
    {
        /// <summary>
        /// Determines whether to unlock clan troops and performs unlock according to settings.
        /// </summary>
        public static void TryUnlockClanTroops(WClan clan, bool fromBootstrap)
        {
            if (clan?.Base == null)
                return;

            switch (Configuration.ClanTroopsUnlock.Value)
            {
                case Configuration.TroopsUnlockMode.Disabled:
                    return;

                case Configuration.TroopsUnlockMode.AlwaysUnlocked:
                    UnlockFactionTroops(clan, label: "clan", fromBootstrap: fromBootstrap);
                    return;

                case Configuration.TroopsUnlockMode.UnlockedWithFirstFief:
                    if (clan.HasFiefs)
                        UnlockFactionTroops(clan, label: "clan", fromBootstrap: fromBootstrap);
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// Determines whether to unlock kingdom troops and performs unlock according to settings.
        /// </summary>
        public static void TryUnlockKingdomTroops(WKingdom kingdom, bool fromBootstrap)
        {
            if (kingdom?.Base == null)
                return;

            switch (Configuration.KingdomTroopsUnlock.Value)
            {
                case Configuration.TroopsUnlockMode.Disabled:
                    return;

                case Configuration.TroopsUnlockMode.UnlockedUponBecomingRuler:
                    if (Player.IsRuler)
                        UnlockFactionTroops(
                            kingdom,
                            label: "kingdom",
                            fromBootstrap: fromBootstrap
                        );
                    return;

                default:
                    return;
            }
        }

        /// <summary>
        /// Removes custom troops from a clan by clearing its root troop references.
        /// The clan will revert to using kingdom or culture troops as fallback.
        /// </summary>
        public static void DeleteClanTroops(WClan clan)
        {
            if (clan?.Base == null)
                return;

            clan.SetRootBasic(null);
            clan.SetRootElite(null);

            WCharacter.RefreshRetinueConversions(clan);

            Log.Info($"Deleted custom troops for clan '{clan.Name}'.");
        }

        /// <summary>
        /// Initializes custom troops for a clan by cloning from culture roots (the standard path).
        /// Suppresses the unlock popup since this is triggered on demand from the editor.
        /// </summary>
        public static void InitializeClanTroopsFromCultureRoots(WClan clan)
        {
            if (clan?.Base == null)
                return;

            UnlockFactionTroops(
                clan,
                label: "companion clan",
                fromBootstrap: false,
                suppressPopup: true
            );
        }

        /// <summary>
        /// Initializes custom troops for a clan by cloning from an existing faction's roots.
        /// Falls back to culture roots if the source has no custom troops.
        /// Suppresses the unlock popup since this is triggered on demand from the editor.
        /// </summary>
        public static void InitializeClanTroopsFromSource(WClan clan, IBaseFaction source)
        {
            if (clan?.Base == null)
                return;

            var basicOverride = source?.RootBasic;
            var eliteOverride = source?.RootElite;

            UnlockFactionTroops(
                clan,
                label: "companion clan",
                fromBootstrap: false,
                basicOverride: basicOverride,
                eliteOverride: eliteOverride,
                suppressPopup: true
            );
        }

        /// <summary>
        /// Performs the actual unlocking and registration of faction troop roots, showing popups as needed.
        /// </summary>
        private static void UnlockFactionTroops<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            string label,
            bool fromBootstrap,
            WCharacter basicOverride = null,
            WCharacter eliteOverride = null,
            bool suppressPopup = false
        )
            where TWrapper : BaseMapFaction<TWrapper, TFaction>
            where TFaction : MBObjectBase, IFaction
        {
            if (faction?.MapFaction == null)
                return;

            var culture = faction.Culture;
            if (culture?.Base == null)
                return;

            bool changed = false;

            WCharacter createdBasic = null;
            WCharacter createdElite = null;

            // Suppress CharacterCloner unlock popups when running from bootstrap.
            var unlockSink = !fromBootstrap ? new List<WItem>(128) : null;

            if (faction.RootBasic == null)
            {
                var basicTemplate = basicOverride ?? culture.RootBasic;
                Log.Info(
                    $"Creating faction basic troops for '{faction.Name}' from culture '{culture.Name}'."
                );
                Log.Info(
                    $"Template basic troop: '{basicTemplate?.Name ?? "null"}' ({basicTemplate?.StringId ?? "null"})"
                );

                createdBasic = CreateFromCultureRoot(
                    template: basicTemplate,
                    factionName: faction.Name,
                    culture: culture,
                    unlockSink: unlockSink,
                    isEliteLine: false
                );

                if (createdBasic?.Base != null)
                {
                    faction.SetRootBasic(createdBasic);
                    changed = true;
                }
            }

            if (faction.RootElite == null)
            {
                var eliteTemplate = eliteOverride ?? culture.RootElite;
                Log.Info(
                    $"Creating faction elite troops for '{faction.Name}' from culture '{culture.Name}'."
                );
                Log.Info(
                    $"Template elite troop: '{eliteTemplate?.Name ?? "null"}' ({eliteTemplate?.StringId ?? "null"})"
                );

                createdElite = CreateFromCultureRoot(
                    template: eliteTemplate,
                    factionName: faction.Name,
                    culture: culture,
                    unlockSink: unlockSink,
                    isEliteLine: true
                );

                if (createdElite?.Base != null)
                {
                    faction.SetRootElite(createdElite);
                    changed = true;
                }
            }

            if (!changed)
                return;

            Log.Info($"Unlocked {label} troops for '{faction.Name}'.");

            // Ensure existing retinues immediately see the new clan/kingdom troops as valid conversions.
            WCharacter.RefreshRetinueConversions(faction);

            if (unlockSink != null && unlockSink.Count > 0)
            {
                // Single merged popup sentence, deduped by UnlockNotifier.
                ItemUnlockNotifier.ItemsUnlocked(
                    ItemUnlockNotifier.UnlockMethod.Troops,
                    unlockSink
                );
            }

            // Popup only for event-driven unlocks (not bootstrap or editor-initiated).
            if (fromBootstrap || suppressPopup)
                return;

            // Show representative troop in popup.
            var select = createdElite ?? createdBasic ?? faction.RootElite ?? faction.RootBasic;
            if (select?.Base == null)
                return;

            var count = 0;

            if (createdElite != null)
                count += createdElite.Tree.Count;

            if (createdBasic != null)
                count += createdBasic.Tree.Count;

            ShowUnlockPopup(faction, select, count);
        }

        /// <summary>
        /// Creates cloned troops from a culture root according to configured starter mode.
        /// </summary>
        private static WCharacter CreateFromCultureRoot(
            WCharacter template,
            string factionName,
            WCulture culture,
            List<WItem> unlockSink,
            bool isEliteLine = false
        )
        {
            if (template?.Base == null)
                return null;

            WCharacter created;

            var mode = Configuration.StarterTroops.Value;

            if (mode == Configuration.TroopsMode.RootsOnly)
            {
                created = Cloner.CloneTroop(
                    template,
                    skills: true,
                    equipments: true,
                    intoStub: null,
                    unlockItems: true,
                    notifyUnlocks: false,
                    unlockSink: unlockSink
                );
            }
            else
            {
                bool lean = mode == Configuration.TroopsMode.LeanTrees;

                created = Cloner.CloneTreeFromRoot(
                    template,
                    lean: lean,
                    skills: true,
                    equipments: true,
                    notifyUnlocks: false,
                    unlockSink: unlockSink
                );

                // Ensure mariner flag is false for lean faction troops.
                created.IsMariner = false;
            }

            if (created?.Base == null)
                return null;

            if (mode == Configuration.TroopsMode.FullTrees)
            {
                var tree = created.Tree ?? [created];
                for (int i = 0; i < tree.Count; i++)
                {
                    var node = tree[i];
                    if (node?.Base == null)
                        continue;

                    node.Name = BuildFactionTroopName(node.Name, factionName, culture);
                    node.HiddenInEncyclopedia = false;
                }
            }
            else if (mode == Configuration.TroopsMode.LeanTrees)
            {
                var nobleLine =
                    isEliteLine
                    || (
                        culture?.RootElite?.Base != null
                        && template?.Base != null
                        && template.StringId == culture.RootElite.StringId
                    );

                Cloner.ApplyLeanFactionNames(created, factionName, nobleLine: nobleLine);

                var tree = created.Tree ?? [created];
                for (int i = 0; i < tree.Count; i++)
                {
                    var node = tree[i];
                    if (node?.Base == null)
                        continue;

                    node.HiddenInEncyclopedia = false;
                }
            }
            else
            {
                created.Name = BuildFactionTroopName(created.Name, factionName, culture);
                created.HiddenInEncyclopedia = false;
            }

            return created;
        }
    }
}
