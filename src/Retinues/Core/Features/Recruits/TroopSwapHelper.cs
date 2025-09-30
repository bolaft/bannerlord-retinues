using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Features.Recruits
{
    public static class TroopSwapHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Integrity Checks                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        public static bool IsValidChar(CharacterObject c) =>
            c != null
            && new WCharacter(c).IsActive
            && !string.IsNullOrEmpty(c.StringId)
            && c.Name != null;

        public static bool IsValid(WCharacter w) => w != null && w.IsActive;

        public static bool LooksCorrupt(CharacterObject c) =>
            c == null || !new WCharacter(c).IsActive || string.IsNullOrEmpty(c.StringId) || c.Name == null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Tree Utilities                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsEliteLine(WCharacter troop)
        {
            try
            {
                if (!IsValid(troop) || troop.Culture?.Base?.EliteBasicTroop == null) return false;
                var eliteRoot = new WCharacter(troop.Culture.Base.EliteBasicTroop);
                return InTree(eliteRoot, troop.StringId);
            }
            catch { return false; }
        }

        public static bool InTree(WCharacter root, string id) =>
            IsValid(root) && root.Tree.Any(n => n.StringId == id);

        public static WCharacter GetFactionRootFor(WCharacter vanilla, WFaction faction)
        {
            if (!IsValid(vanilla) || faction == null) return null;

            var root = IsEliteLine(vanilla) ? faction.RootElite : faction.RootBasic;
            return IsValid(root) ? root : null;
        }

        // BFS: best tier (exact if possible, else closest active node)
        public static WCharacter MatchTier(WCharacter root, int targetTier)
        {
            if (!IsValid(root)) return null;

            var best = root;
            var bestDelta = Math.Abs(root.Tier - targetTier);

            var q = new Queue<WCharacter>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (!IsValid(n)) continue;

                var delta = Math.Abs(n.Tier - targetTier);
                if (delta < bestDelta) { best = n; bestDelta = delta; }
                if (n.Tier == targetTier) return n;

                foreach (var ch in n.UpgradeTargets) q.Enqueue(ch);
            }
            return best;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Faction Helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsFactionTroop(WFaction faction, WCharacter troop)
        {
            if (faction == null || !IsValid(troop)) return false;

            bool idMatch(string id) => id == troop.StringId;

            if (IsValid(faction.RetinueElite) && idMatch(faction.RetinueElite.StringId)) return true;
            if (IsValid(faction.RetinueBasic) && idMatch(faction.RetinueBasic.StringId)) return true;
            if (IsValid(faction.RootElite) && idMatch(faction.RootElite.StringId)) return true;
            if (IsValid(faction.RootBasic) && idMatch(faction.RootBasic.StringId)) return true;

            var inElite = faction.EliteTroops?.Any(t => IsValid(t) && idMatch(t.StringId)) == true;
            if (inElite) return true;

            var inBasic = faction.BasicTroops?.Any(t => IsValid(t) && idMatch(t.StringId)) == true;
            return inBasic;
        }

        // Map a vanilla troop to the best faction replacement (or null if no safe replacement).
        public static WCharacter FindReplacement(WCharacter vanilla, WFaction faction)
        {
            if (!IsValid(vanilla) || faction == null) return null;

            // If vanilla already belongs to faction, no replacement
            if (IsFactionTroop(faction, vanilla)) return null;

            var root = GetFactionRootFor(vanilla, faction);
            if (!IsValid(root)) return null;

            var candidate = MatchTier(root, vanilla.Tier);
            return IsValid(candidate) ? candidate : null;
        }

        // Resolve the "target faction" for a recruiting hero: prefer clan, then kingdom.
        public static WFaction ResolveTargetFaction(Hero recruiter)
        {
            try
            {
                var clan = recruiter?.Clan;
                var kingdom = clan?.Kingdom;
                var playerClan = Player.Clan;
                var playerKingdom = Player.Kingdom;

                // User setting decides priority
                bool preferClan = Config.GetOption<bool>("ClanTroopsOverKingdomTroops");

                if (preferClan)
                {
                    if (playerClan != null && clan != null && clan.StringId == playerClan.StringId) return playerClan;
                    if (playerKingdom != null && kingdom != null && kingdom.StringId == playerKingdom.StringId) return playerKingdom;
                }
                else
                {
                    if (playerKingdom != null && kingdom != null && kingdom.StringId == playerKingdom.StringId) return playerKingdom;
                    if (playerClan != null && clan != null && clan.StringId == playerClan.StringId) return playerClan;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "ResolveTargetFaction failed.");
            }
            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Fallback                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static CharacterObject SafeVanillaFallback(Settlement s, WCharacter like = null)
        {
            try
            {
                // prefer settlement culture basic
                var basic = s?.Culture?.BasicTroop;
                if (IsValidChar(basic)) return basic;

                // else volunteer/militia basics by culture
                var m = s?.Culture?.MeleeMilitiaTroop;
                if (IsValidChar(m)) return m;

                // absolute minimum: villager of settlement culture
                var v = s?.Culture?.Villager;
                if (IsValidChar(v)) return v;
            }
            catch { /* ignore */ }

            // If literally nothing else, looter (is registered in all games)
            var looter = CharacterObject.Find("looter");
            return IsValidChar(looter) ? looter : null;
        }
    }
}
