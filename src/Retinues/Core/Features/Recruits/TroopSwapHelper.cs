using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Features.Recruits
{
    public static class TroopSwapHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Trees                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsEliteLine(WCharacter troop)
        {
            if (troop?.Culture?.Base?.EliteBasicTroop == null)
                return false;

            var eliteRoot = new WCharacter(troop.Culture.Base.EliteBasicTroop);
            return InTree(eliteRoot, troop.StringId);
        }

        public static bool InTree(WCharacter root, string id) =>
            root != null && root.Tree.Any(n => n.StringId == id);

        public static WCharacter GetFactionRootFor(WCharacter vanilla, WFaction faction)
        {
            if (vanilla == null || faction == null)
                return null;

            var isElite = IsEliteLine(vanilla);
            var troop = isElite ? faction.RootElite : faction.RootBasic;

            // Heuristic to check if initialized
            if (troop == null || troop.StringId == troop.Name || !troop.IsActive)
                return null;

            return troop;
        }

        // Find a node in the faction tree that best matches the requested tier.
        // Uses BFS; exact tier if possible, else the closest tier by absolute distance.
        public static WCharacter MatchTier(WCharacter root, int targetTier)
        {
            if (root == null)
                return null;

            var best = root;
            var bestDelta = Math.Abs(root.Tier - targetTier);

            var q = new Queue<WCharacter>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                var delta = Math.Abs(n.Tier - targetTier);

                if (delta < bestDelta && n.IsActive)
                {
                    best = n;
                    bestDelta = delta;
                }

                if (n.Tier == targetTier)
                    return n;

                foreach (var ch in n.UpgradeTargets)
                    q.Enqueue(ch);
            }

            return best;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsFactionTroop(WFaction faction, WCharacter troop)
        {
            if (faction == null || troop == null)
                return false;

            if (faction.RetinueElite?.StringId == troop.StringId)
                return true;
            if (faction.RetinueBasic?.StringId == troop.StringId)
                return true;
            if (faction.RootElite?.StringId == troop.StringId)
                return true;
            if (faction.RootBasic?.StringId == troop.StringId)
                return true;

            // Null-safe list checks:
            var inElite =
                faction.EliteTroops != null
                && faction.EliteTroops.Any(t => t.StringId == troop.StringId);
            if (inElite)
                return true;

            var inBasic =
                faction.BasicTroops != null
                && faction.BasicTroops.Any(t => t.StringId == troop.StringId);
            return inBasic;
        }

        // Find the appropriate replacement in the player's custom tree for a vanilla troop.
        public static WCharacter FindReplacement(WCharacter vanilla, WFaction faction)
        {
            if (vanilla == null || faction == null)
                return null;

            // If it’s already one of our custom troops, no swap is needed.
            if (IsFactionTroop(faction, vanilla))
                return null;

            var root = GetFactionRootFor(vanilla, faction);
            if (root == null)
                return null;

            return MatchTier(root, vanilla.Tier);
        }

        public static WFaction ResolveTargetFaction(Hero recruiter)
        {
            if (recruiter?.Clan == null)
                return null;

            var playerClan = Player.Clan;
            var playerKingdom = Player.Kingdom;
            bool clanTroopsOverKingdomTroops = Config.GetOption<bool>("ClanTroopsOverKingdomTroops");

            if (clanTroopsOverKingdomTroops)
            {
                if (playerClan != null && recruiter.Clan.StringId == playerClan.StringId)
                    return playerClan;
                if (
                    playerKingdom != null
                    && recruiter.Clan?.Kingdom != null
                    && recruiter.Clan.Kingdom.StringId == playerKingdom.StringId
                )
                    return playerKingdom;
            }
            else
            {
                if (
                    playerKingdom != null
                    && recruiter.Clan?.Kingdom != null
                    && recruiter.Clan.Kingdom.StringId == playerKingdom.StringId
                )
                    return playerKingdom;
                if (playerClan != null && recruiter.Clan.StringId == playerClan.StringId)
                    return playerClan;
            }
            return null;
        }
    }
}
