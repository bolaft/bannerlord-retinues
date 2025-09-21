using System;
using System.Collections.Generic;
using Retinues.Core.Features.Xp;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Core.Editor
{
    public static class TroopManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Collection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<WCharacter> CollectRetinueTroops(WFaction faction)
        {
            return [faction.RetinueElite, faction.RetinueBasic];
        }

        public static List<WCharacter> CollectEliteTroops(WFaction faction)
        {
            return [.. faction.EliteTroops];
        }

        public static List<WCharacter> CollectBasicTroops(WFaction faction)
        {
            return [.. faction.BasicTroops];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Rename(WCharacter troop, string newName)
        {
            troop.Name = newName.Trim();
        }

        public static void ChangeGender(WCharacter troop)
        {
            troop.IsFemale = !troop.IsFemale;
        }

        public static void ModifySkill(WCharacter troop, SkillObject skill, int delta)
        {
            if (troop == null || skill == null || delta == 0)
                return;

            int current = troop.GetSkill(skill);

            if (delta > 0)
            {
                // Cost to go from current -> current + 1
                int cost = TroopRules.SkillPointXpCost(current);
                if (!TroopXpService.TrySpend(troop, cost))
                    return; // Not enough XP; a CanIncrement gate should already prevent this
                troop.SetSkill(skill, current + 1);
            }
            else // delta < 0
            {
                // Respect your existing "CanDecrement" constraints upstream
                int newValue = current - 1;
                if (newValue < 0)
                    return;

                // Refund the cost of the point we're removing (i.e., the cost that was paid to go from newValue -> newValue + 1)
                int refund = TroopRules.SkillPointXpCost(newValue);
                troop.SetSkill(skill, newValue);
                TroopXpService.Refund(troop, refund);
            }
        }

        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            // Determine the position in the tree
            List<int> path = [.. troop.PositionInTree, troop.UpgradeTargets.Length];

            // Wrap the custom troop
            var child = new WCharacter(troop.Faction == Player.Kingdom, troop.IsElite, false, path);

            // Copy from the original troop
            child.FillFrom(
                troop,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: true
            );

            // Set name and level
            child.Name = targetName.Trim();
            child.Level = troop.Level + 5;

            // Add to upgrade targets of the parent
            troop.AddUpgradeTarget(child);

            // Activate
            child.Activate();

            return child;
        }

        public static void Remove(WCharacter troop)
        {
            // Stock the troop's equipment
            foreach (var item in troop.Equipment.Items)
                item.Stock();

            // Remove the troop
            troop.Remove();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RankUp(WCharacter troop)
        {
            if (troop.IsMaxTier)
                return;

            int cost = TroopRules.RankUpCost(troop);

            if (Player.Gold < cost)
                return;

            // Pay the cost
            Player.ChangeGold(-cost);

            // Spend the XP
            TroopXpService.TrySpend(troop, cost);

            // Upgrade the troop
            troop.Level += 5;
        }

        public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
        {
            List<WCharacter> sources = [];
            WCharacter cultureRoot;
            WCharacter factionRoot;

            if (retinue == retinue.Faction?.RetinueElite)
            {
                cultureRoot = retinue.Culture.RootElite;
                factionRoot = retinue.Faction.RootElite;
            }
            else if (retinue == retinue.Faction.RetinueBasic)
            {
                cultureRoot = retinue.Culture.RootBasic;
                factionRoot = retinue.Faction.RootBasic;
            }
            else
                return sources;

            if (cultureRoot != null)
                foreach (WCharacter troop in cultureRoot.Tree)
                    if (IsEligibleForRetinue(troop, retinue))
                    {
                        sources.Add(troop);
                        break; // Only take one
                    }

            if (factionRoot != null)
                foreach (WCharacter troop in factionRoot.Tree)
                    if (IsEligibleForRetinue(troop, retinue))
                    {
                        sources.Add(troop);
                        break; // Only take one
                    }

            return sources;
        }

        private static bool IsEligibleForRetinue(WCharacter troop, WCharacter retinue)
        {
            // Basic checks
            if (!retinue.IsRetinue || troop.IsRetinue)
                return false;

            // Check for culture match
            if (troop.Culture?.StringId != retinue.Culture?.StringId)
                return false;

            // Check for tier match
            if (troop.Tier != retinue.Tier)
                return false;

            return true;
        }

        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            // Number of 'from' troops available in party
            int maxConvertible = Player.Party.MemberRoster.CountOf(from);

            if (to.IsRetinue)
            {
                // Number of 'to' troops already in party
                int currentRetinue = Player.Party.MemberRoster.CountOf(to);

                // Cap left for retinue troops
                int cap = TroopRules.RetinueCapFor(to);

                // Apply cap
                maxConvertible = Math.Min(maxConvertible, Math.Max(0, cap - currentRetinue));
            }

            return maxConvertible;
        }

        public static void Convert(
            WCharacter from,
            WCharacter to,
            int amountRequested,
            int cost = 0
        )
        {
            // Clamp to max possible
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);

            // Apply cost
            Player.ChangeGold(-cost);

            // Mutate roster
            Player.Party.MemberRoster.RemoveTroop(from, amount);
            Player.Party.MemberRoster.AddTroop(to, amount);
        }
    }
}
