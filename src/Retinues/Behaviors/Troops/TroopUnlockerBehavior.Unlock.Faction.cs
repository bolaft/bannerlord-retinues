using System.Collections.Generic;
using Retinues.Behaviors.Unlocks;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Base;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
        private static void TryUnlockClanTroops(WClan clan, bool fromBootstrap)
        {
            if (clan?.Base == null)
                return;

            switch (Settings.ClanTroopsUnlock.Value)
            {
                case Settings.ClanTroopsUnlockMode.Disabled:
                    return;

                case Settings.ClanTroopsUnlockMode.AlwaysUnlocked:
                    UnlockFactionTroops(clan, label: "clan", fromBootstrap: fromBootstrap);
                    return;

                case Settings.ClanTroopsUnlockMode.UnlockedWithFirstFief:
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
        private static void TryUnlockKingdomTroops(WKingdom kingdom, bool fromBootstrap)
        {
            if (kingdom?.Base == null)
                return;

            switch (Settings.KingdomTroopsUnlock.Value)
            {
                case Settings.KingdomTroopsUnlockMode.Disabled:
                    return;

                case Settings.KingdomTroopsUnlockMode.AlwaysUnlocked:
                    UnlockFactionTroops(kingdom, label: "kingdom", fromBootstrap: fromBootstrap);
                    return;

                case Settings.KingdomTroopsUnlockMode.UnlockedUponBecomingRuler:
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
        /// Performs the actual unlocking and registration of faction troop roots, showing popups as needed.
        /// </summary>
        private static void UnlockFactionTroops<TWrapper, TFaction>(
            BaseMapFaction<TWrapper, TFaction> faction,
            string label,
            bool fromBootstrap
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
                Log.Info(
                    $"Creating faction basic troops for '{faction.Name}' from culture '{culture.Name}'."
                );
                Log.Info(
                    $"Template basic troop: '{culture.RootBasic?.Name ?? "null"}' ({culture.RootBasic?.StringId ?? "null"})"
                );

                createdBasic = CreateFromCultureRoot(
                    template: culture.RootBasic,
                    factionName: faction.Name,
                    culture: culture,
                    unlockSink: unlockSink
                );

                if (createdBasic?.Base != null)
                {
                    faction.SetRootBasic(createdBasic);
                    changed = true;
                }
            }

            if (faction.RootElite == null)
            {
                Log.Info(
                    $"Creating faction elite troops for '{faction.Name}' from culture '{culture.Name}'."
                );
                Log.Info(
                    $"Template elite troop: '{culture.RootElite?.Name ?? "null"}' ({culture.RootElite?.StringId ?? "null"})"
                );

                createdElite = CreateFromCultureRoot(
                    template: culture.RootElite,
                    factionName: faction.Name,
                    culture: culture,
                    unlockSink: unlockSink
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

            // Popup only for event-driven unlocks (not bootstrap).
            if (fromBootstrap)
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
            List<WItem> unlockSink
        )
        {
            if (template?.Base == null)
                return null;

            WCharacter created;

            var mode = Settings.StarterTroops.Value;

            if (mode == Settings.TroopsMode.RootsOnly)
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
                bool lean = mode == Settings.TroopsMode.LeanTrees;

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

            if (mode == Settings.TroopsMode.FullTrees)
            {
                var tree = created.Tree ?? new List<WCharacter> { created };
                for (int i = 0; i < tree.Count; i++)
                {
                    var node = tree[i];
                    if (node?.Base == null)
                        continue;

                    node.Name = BuildFactionTroopName(node.Name, factionName, culture);
                    node.HiddenInEncyclopedia = false;
                }
            }
            else if (mode == Settings.TroopsMode.LeanTrees)
            {
                var nobleLine =
                    culture?.RootElite?.Base != null
                    && template?.Base != null
                    && template.StringId == culture.RootElite.StringId;

                Cloner.ApplyLeanFactionNames(created, factionName, nobleLine: nobleLine);

                var tree = created.Tree ?? new List<WCharacter> { created };
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
