using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
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
        public static WCharacter FindReplacement(WCharacter vanilla, WFaction faction, bool militia = false)
        {
            if (!IsValid(vanilla) || faction == null) return null;

            // If vanilla already belongs to faction, no replacement
            if (IsFactionTroop(faction, vanilla)) return null;

            // If militia flag is set, replace using militia lines if available
            if (militia)
            {
                List<WCharacter> candidates =
                [
                    faction.MilitiaMelee,
                    faction.MilitiaMeleeElite,
                    faction.MilitiaRanged,
                    faction.MilitiaRangedElite
                ];

                foreach (var c in candidates)
                {
                    if (!IsValid(c)) continue;

                    if (c.Tier != vanilla.Tier) continue;
                    if (c.IsRanged != vanilla.IsRanged) continue;
                    if (c.IsElite != vanilla.IsElite) continue;

                    return c; // exact match
                }
            }

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

        public static CharacterObject SafeVanillaFallback(Settlement s)
        {
            try
            {
                // prefer settlement culture basic
                var basic = s?.Culture?.BasicTroop;
                if (IsValidChar(basic)) return basic;

                // absolute minimum: villager of settlement culture
                var v = s?.Culture?.Villager;
                if (IsValidChar(v)) return v;
            }
            catch { /* ignore */ }

            // If literally nothing else, looter (is registered in all games)
            var looter = CharacterObject.Find("looter");
            return IsValidChar(looter) ? looter : null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rosters                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SwapParty(MobileParty p)
        {
            try
            {
                WParty party = new(p);
                WFaction faction;

                if (party.IsPlayerKingdomParty)
                {
                    Log.Info($"TroopSwapHelper: Party '{party.Name}' is a player kingdom party. Using Player.Kingdom.");
                    faction = Player.Kingdom;
                }
                else if (party.IsPlayerClanParty)
                {
                    Log.Info($"TroopSwapHelper: Party '{party.Name}' is a player clan party. Using Player.Clan.");
                    faction = Player.Clan;
                }
                else
                {
                    Log.Info($"TroopSwapHelper: Party '{party.Name}' (clan: {party.Clan?.Name}, kingdom: {party.Kingdom?.Name}) is not a player faction party. Skipping swap.");
                    return; // only swap for player factions
                }

                Log.Info($"TroopSwapHelper: Swapping roster for party '{party.Name}' with faction '{faction?.Name ?? "NULL"}'.");
                SwapRoster(party, faction);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"TroopSwapHelper: Failed swapping party '{(p?.Name != null ? p.Name.ToString() : "NULL")}'.");
                Log.Error($"Clan: {p.ActualClan?.Name} ({p.ActualClan?.StringId}) Kingdom: {p.ActualClan?.Kingdom?.Name} ({p.ActualClan?.Kingdom?.StringId})");
            }
        }

        public static int SwapRoster(WParty party, WFaction faction)
        {
            int replaced = 0;
            try
            {
                if (party.Base == null || party.MemberRoster == null || faction == null) return 0;

                // Build a fresh roster to avoid in-place index/XpChanged issues.
                var dst = new TroopRoster(party.Base.Party);

                // Enumerate snapshot so we don't fight internal mutations.
                foreach (var e in party.MemberRoster.Base.GetTroopRoster())
                {
                    var ch = e.Character;
                    if (ch == null || e.Number <= 0)
                        continue;

                    // never touch heroes
                    if (ch.IsHero)
                    {
                        dst.AddToCounts(ch, e.Number, insertAtFront: false, woundedCount: e.WoundedNumber, xpChange: e.Xp);
                        continue;
                    }

                    // Wrap and look for a faction replacement using your helpers
                    var vanilla = new WCharacter(ch);
                    var replacement = FindReplacement(vanilla, faction, militia: party.IsMilitia);

                    // If no good replacement, keep as-is
                    if (replacement == null || !replacement.IsActive || replacement.StringId == vanilla.StringId)
                    {
                        dst.AddToCounts(ch, e.Number, insertAtFront: false, woundedCount: e.WoundedNumber, xpChange: e.Xp);
                        continue;
                    }

                    // Apply replacement, preserving totals
                    dst.AddToCounts(replacement.Base, e.Number, insertAtFront: false, woundedCount: e.WoundedNumber, xpChange: e.Xp);
                    replaced++;
                }

                // Swap onto PartyBase (properties are read-only; use same reflection approach as your sanitizer)
                Reflector.SetPropertyValue(party.Base.Party, "MemberRoster", dst);

                Log.Debug($"TroopSwapHelper.SwapRoster: Members swapped for {party?.Name}; stacks replaced={replaced}.");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            return replaced;
        }
    }
}
