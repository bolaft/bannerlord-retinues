using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Game;

namespace Retinues.Core.Editor
{
    public static class TroopManager
    {
        // ================================================================
        // Collection
        // ================================================================

        public static List<WCharacter> CollectRetinueTroops(WFaction faction)
        {
            return [faction.RetinueElite, faction.RetinueBasic];
        }

        public static List<WCharacter> CollectEliteTroops(WFaction faction)
        {
            return faction.EliteTroops;
        }

        public static List<WCharacter> CollectBasicTroops(WFaction faction)
        {
            return faction.BasicTroops;
        }

        // ================================================================
        // All Troops
        // ================================================================

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
            troop.SetSkill(skill, troop.GetSkill(skill) + delta);
        }

        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            // Create the new troop by cloning
            var target = troop.Clone(
                faction: troop.Faction,
                parent: troop,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: true
            );

            // Set name and level
            target.Name = targetName.Trim();
            target.Level = troop.Level + 5;

            // Add it the the faction's troop list
            if (target.IsElite)
                troop.Faction.EliteTroops.Add(target);
            else
                troop.Faction.BasicTroops.Add(target);

            return target;
        }

        public static void Remove(WCharacter troop)
        {
            // Stock the troop's equipment
            foreach (var item in troop.Equipment.Items)
                item.Stock();

            // Remove the troop
            troop.Remove();
        }

        // ================================================================
        // Retinues
        // ================================================================

        public static void RankUp(WCharacter troop)
        {
            if (troop.IsMaxTier) return;

            int cost = TroopRules.RankUpCost(troop);

            if (Player.Gold < cost) return;

            // Pay the cost
            Player.ChangeGold(-cost);

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
                cultureRoot = retinue.Culture?.RootElite;
                factionRoot = retinue.Faction?.RootElite;
            }
            else if (retinue == retinue.Faction?.RetinueBasic)
            {
                cultureRoot = retinue.Culture?.RootBasic;
                factionRoot = retinue.Faction?.RootBasic;
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
            if (!retinue.IsRetinue || troop.IsRetinue) return false;

            // Check for culture match
            if (troop.Culture?.StringId != retinue.Culture?.StringId) return false;

            // Check for tier match
            if (troop.Tier != retinue.Tier) return false;

            return true;
        }

        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            // Number of 'from' troops available in party
            int maxConvertible = Player.Party.Roster.CountOf(from);

            if (to.IsRetinue)
            {
                // Number of 'to' troops already in party
                int currentRetinue = Player.Party.Roster.CountOf(to);

                // Cap left for retinue troops
                int cap = TroopRules.RetinueCapFor(to);

                // Apply cap
                maxConvertible = Math.Min(maxConvertible, Math.Max(0, cap - currentRetinue));
            }

            return maxConvertible;
        }

        public static void Convert(WCharacter from, WCharacter to, int amountRequested, int cost = 0)
        {
            // Clamp to max possible
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);

            // Apply cost
            Player.ChangeGold(-cost);

            // Mutate roster
            Player.Party.Roster.RemoveTroop(from, amount);
            Player.Party.Roster.AddTroop(to, amount);
        }
    }
}
