using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor;
using Retinues.Framework.Behaviors;
using Retinues.Game.Unlocks;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Troops
{
    /// <summary>
    /// Unlocks and assigns custom troops for the player clan and player kingdom.
    /// Clones culture roots or whole culture trees, then stores them on the faction wrappers.
    /// </summary>
    public sealed class TroopUnlockBehavior : BaseCampaignBehavior<TroopUnlockBehavior>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Auto Handlers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnCharacterCreationIsOver() =>
            TryUnlockFromCurrentState(fromBootstrap: true);

        protected override void OnGameLoadFinished() =>
            TryUnlockFromCurrentState(fromBootstrap: true);

        protected override void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail
        )
        {
            try
            {
                if (newOwner?.Clan == null)
                    return;

                var playerClan = Player.Clan;
                if (playerClan?.Base == null)
                    return;

                if (newOwner.Clan != playerClan.Base)
                    return;

                // Event-driven unlock: should popup.
                TryUnlockClanTroops(playerClan, fromBootstrap: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        protected override void OnKingdomCreated(Kingdom kingdom)
        {
            try
            {
                var playerKingdom = Player.Kingdom;
                if (playerKingdom?.Base == null)
                    return;

                if (kingdom != playerKingdom.Base)
                    return;

                // Event-driven unlock: should popup.
                TryUnlockKingdomTroops(playerKingdom, fromBootstrap: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unlock                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void TryUnlockNow(bool fromBootstrap = false)
        {
            if (!TryGetInstance(out var behavior) || behavior == null)
                return;

            behavior.TryUnlockFromCurrentState(fromBootstrap);
        }

        private void TryUnlockFromCurrentState(bool fromBootstrap)
        {
            TryUnlockClanTroops(Player.Clan, fromBootstrap);
            TryUnlockKingdomTroops(Player.Kingdom, fromBootstrap);
        }

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

        private static void UnlockFactionTroops(
            BaseMapFactionWrapper faction,
            string label,
            bool fromBootstrap
        )
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
            var notifyUnlocks = !fromBootstrap;
            var unlockSink = (!fromBootstrap && notifyUnlocks) ? new List<WItem>(128) : null;

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
            WCharacter.RefreshRetinueConversions(faction.Faction);

            if (unlockSink != null && unlockSink.Count > 0)
            {
                // Single merged popup sentence, deduped by UnlockNotifier.
                UnlockNotifier.ItemsUnlocked(UnlockNotifier.UnlockMethod.Troops, unlockSink);
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

            ShowUnlockPopup(faction.Faction, select, count);
        }

        private static void ShowUnlockPopup(IBaseFaction faction, WCharacter select, int count)
        {
            var factionType = L.T("faction_type_faction", "faction");

            if (faction is WKingdom)
                factionType = L.T("faction_type_kingdom", "kingdom");
            else if (faction is WClan)
                factionType = L.T("faction_type_clan", "clan");

            var title = L.T("faction_troops_unlocked_title", "{FACTION} Troops")
                .SetTextVariable("FACTION", faction.Name);

            var desc = L.T(
                    "faction_troops_unlocked_desc",
                    "{COUNT} new troops are now available for the {FACTION} {FACTION_TYPE}."
                )
                .SetTextVariable("COUNT", count)
                .SetTextVariable("FACTION", faction.Name)
                .SetTextVariable("FACTION_TYPE", factionType);

            var go = L.T("go_to_editor", "Go to editor");
            var ok = GameTexts.FindText("str_ok");

            Inquiries.Popup(
                title,
                onChoice1: () =>
                    EditorLauncher.Launch(
                        EditorLaunchArgs.Player(faction: faction, character: select)
                    ),
                onChoice2: () => { },
                choice1Text: go,
                choice2Text: ok,
                description: desc,
                pauseGame: true,
                delayUntilOnWorldMap: true
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Clone Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                created = TroopCloner.CloneVanilla(
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

                created = TroopCloner.CloneTreeFromRoot(
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
            else if (mode == Settings.TroopsMode.LeanTrees)
            {
                var nobleLine =
                    culture?.RootElite?.Base != null
                    && template?.Base != null
                    && template.StringId == culture.RootElite.StringId;

                TroopCloner.ApplyLeanFactionNames(created, factionName, nobleLine: nobleLine);

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Naming                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string BuildFactionTroopName(
            string templateName,
            string factionName,
            WCulture culture
        )
        {
            var baseName = (templateName ?? string.Empty).Trim();
            var prefix = (factionName ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(prefix))
                return baseName;

            var stripped = StripCulturePrefix(baseName, culture);

            if (string.IsNullOrEmpty(stripped))
                return prefix;

            if (stripped.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                return stripped;

            return $"{prefix} {stripped}";
        }

        private static string StripCulturePrefix(string name, WCulture culture)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var s = name.Trim();
            if (culture?.Base == null)
                return s;

            var cultureName = (culture.Name ?? string.Empty).Trim();
            var prefixes = BuildCulturePrefixCandidates(cultureName, culture.StringId);

            for (int i = 0; i < prefixes.Count; i++)
            {
                var p = prefixes[i];
                if (string.IsNullOrEmpty(p))
                    continue;

                if (s.StartsWith(p + " ", StringComparison.OrdinalIgnoreCase))
                    return s.Substring(p.Length + 1).Trim();
            }

            return s;
        }

        private static List<string> BuildCulturePrefixCandidates(
            string cultureName,
            string cultureId
        )
        {
            var list = new List<string>();

            if (!string.IsNullOrEmpty(cultureName))
                list.Add(cultureName);

            if (
                !string.IsNullOrEmpty(cultureName)
                && cultureName.EndsWith("a", StringComparison.OrdinalIgnoreCase)
            )
                list.Add(cultureName + "n");

            if (
                !string.IsNullOrEmpty(cultureName)
                && cultureName.EndsWith("ia", StringComparison.OrdinalIgnoreCase)
            )
                list.Add(cultureName.Substring(0, cultureName.Length - 2) + "ian");

            if (
                !string.IsNullOrEmpty(cultureId)
                && cultureId.Equals("empire", StringComparison.OrdinalIgnoreCase)
            )
            {
                list.Add("Imperial");
                list.Add("Empire");
            }

            list.Sort((a, b) => (b?.Length ?? 0).CompareTo(a?.Length ?? 0));
            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Adapter Interface                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private abstract class BaseMapFactionWrapper
        {
            public abstract IFaction MapFaction { get; }
            public abstract IBaseFaction Faction { get; }
            public abstract string Name { get; }
            public abstract WCulture Culture { get; }
            public abstract WCharacter RootBasic { get; }
            public abstract WCharacter RootElite { get; }
            public abstract void SetRootBasic(WCharacter root);
            public abstract void SetRootElite(WCharacter root);
        }

        private sealed class ClanAdapter(WClan clan) : BaseMapFactionWrapper
        {
            private readonly WClan _clan = clan;

            public override IFaction MapFaction => _clan.MapFaction;
            public override IBaseFaction Faction => _clan;
            public override string Name => _clan.Name;
            public override WCulture Culture => _clan.Culture;
            public override WCharacter RootBasic => _clan.RootBasic;
            public override WCharacter RootElite => _clan.RootElite;

            public override void SetRootBasic(WCharacter root) => _clan.SetRootBasic(root);

            public override void SetRootElite(WCharacter root) => _clan.SetRootElite(root);
        }

        private sealed class KingdomAdapter(WKingdom kingdom) : BaseMapFactionWrapper
        {
            private readonly WKingdom _kingdom = kingdom;

            public override IFaction MapFaction => _kingdom.MapFaction;
            public override IBaseFaction Faction => _kingdom;
            public override string Name => _kingdom.Name;
            public override WCulture Culture => _kingdom.Culture;
            public override WCharacter RootBasic => _kingdom.RootBasic;
            public override WCharacter RootElite => _kingdom.RootElite;

            public override void SetRootBasic(WCharacter root) => _kingdom.SetRootBasic(root);

            public override void SetRootElite(WCharacter root) => _kingdom.SetRootElite(root);
        }

        private static void UnlockFactionTroops(WClan clan, string label, bool fromBootstrap) =>
            UnlockFactionTroops(new ClanAdapter(clan), label, fromBootstrap);

        private static void UnlockFactionTroops(
            WKingdom kingdom,
            string label,
            bool fromBootstrap
        ) => UnlockFactionTroops(new KingdomAdapter(kingdom), label, fromBootstrap);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_clan_troops", "retinues")]
        public static string UnlockClanTroopsCommand(List<string> args)
        {
            var clan = Player.Clan;
            if (clan?.Base == null)
                return "Player clan not found.";

            if (clan.RootBasic != null && clan.RootElite != null)
                return "Player clan troops are already unlocked.";

            UnlockFactionTroops(clan, label: "clan", fromBootstrap: false);
            return "Player clan troops unlocked.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("unlock_kingdom_troops", "retinues")]
        public static string UnlockKingdomTroopsCommand(List<string> args)
        {
            var kingdom = Player.Kingdom;
            if (kingdom?.Base == null)
                return "Player kingdom not found.";

            if (kingdom.RootBasic != null && kingdom.RootElite != null)
                return "Player kingdom troops are already unlocked.";

            UnlockFactionTroops(kingdom, label: "kingdom", fromBootstrap: false);
            return "Player kingdom troops unlocked.";
        }
    }
}
