using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Retinues.Features.Experience;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Troops
{
    /// <summary>
    /// Console cheats for repairing custom troop state in a save.
    /// </summary>
    [SafeClass]
    public static class FactionCheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Scrubs a corrupted save: discards stale troop data for factions the player no
        /// longer controls (e.g. an absorbed rebel kingdom) and releases orphaned custom
        /// troop stubs that are not part of the player's current clan/kingdom trees.
        /// Usage: retinues.scrub_save
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("scrub_save", "retinues")]
        public static string ScrubSave(List<string> args)
        {
            if (Campaign.Current == null)
                return "No campaign loaded.";

            var sb = new StringBuilder();

            // 1) Drop stale kingdom save data (defunct rebel kingdom, etc.).
            var behavior = Campaign.Current.GetCampaignBehavior<FactionBehavior>();
            bool clearedKingdom = behavior?.ClearStaleKingdomData() == true;
            sb.AppendLine(
                clearedKingdom
                    ? "Discarded stale kingdom troop data."
                    : "No stale kingdom troop data to discard."
            );

            // 2) Release orphaned custom stubs not reachable from the player's factions.
            var keep = CollectReachableIds();

            // Every custom stub that currently holds data (registered as active or mapped to
            // a faction) but is not reachable from a live player faction tree is an orphan.
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            occupied.UnionWith(WCharacter.ActiveStubIds);
            occupied.UnionWith(BaseFaction.TroopFactionMap.Keys);

            var orphanIds = occupied
                .Where(id => IsCustomId(id) && !keep.Contains(id))
                .ToList();

            int released = 0;
            foreach (var id in orphanIds)
            {
                try
                {
                    var troop = WCharacter.FromStringId(id);
                    if (troop == null || !troop.IsCustom)
                        continue;

                    // Already released as part of a parent's subtree this pass.
                    if (!troop.IsActive && troop.Faction == null)
                        continue;

                    TroopXpBehavior.Set(troop, 0); // Drop any leftover XP pool for the stub
                    troop.Remove(); // Detach, free stub, replace live instances with culture match
                    released++;
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Scrub: failed to release orphan stub '{id}'.");
                }
            }

            sb.AppendLine($"Released {released} orphaned troop(s).");

            if (clearedKingdom || released > 0)
                sb.AppendLine("Save the game to persist the cleanup.");

            Log.Info($"Scrub: cleared kingdom={clearedKingdom}, released {released} orphan(s).");
            return sb.ToString().TrimEnd();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool IsCustomId(string id) =>
            !string.IsNullOrEmpty(id)
            && (
                id.StartsWith(WCharacter.CustomIdPrefix)
                || id.StartsWith(WCharacter.LegacyCustomIdPrefix)
            );

        /// <summary>
        /// Collects the string ids of every custom troop reachable from the player's clan and
        /// kingdom: all root trees (and their upgrades) plus any bound captain instances.
        /// </summary>
        private static HashSet<string> CollectReachableIds()
        {
            var keep = new HashSet<string>(StringComparer.Ordinal);

            void AddTroop(WCharacter troop)
            {
                if (troop == null || !troop.IsCustom)
                    return;
                if (!keep.Add(troop.StringId))
                    return; // already visited

                var captain = troop.GetExistingCaptain();
                if (captain != null)
                    keep.Add(captain.StringId);
            }

            void AddFaction(WFaction faction)
            {
                if (faction == null)
                    return;

                foreach (RootCategory category in Enum.GetValues(typeof(RootCategory)))
                {
                    var root = faction.GetRoot(category);
                    if (root == null)
                        continue;

                    foreach (var troop in root.Tree)
                        AddTroop(troop);
                }
            }

            AddFaction(Player.Clan);
            AddFaction(Player.Kingdom);

            return keep;
        }
    }
}
