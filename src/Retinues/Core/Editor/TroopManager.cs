using System;
using System.Collections.Generic;
using Retinues.Core.Features.Xp;
using Retinues.Core.Game;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;

namespace Retinues.Core.Editor
{
    [SafeClass]
    public static class TroopManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Collection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<WCharacter> CollectRetinueTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting retinue troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [faction.RetinueElite, faction.RetinueBasic];
            }
            catch
            {
                return [];
            }
        }

        public static List<WCharacter> CollectEliteTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting elite troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [.. faction.EliteTroops];
            }
            catch
            {
                return [];
            }
        }

        public static List<WCharacter> CollectBasicTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting basic troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return [.. faction.BasicTroops];
            }
            catch
            {
                return [];
            }
        }

        public static List<WCharacter> CollectMilitiaTroops(WFaction faction)
        {
            Log.Debug(
                $"Collecting militia troops for faction {faction?.Name} (culture {faction?.Culture?.Name})."
            );
            try
            {
                return
                [
                    faction.MilitiaMelee,
                    faction.MilitiaMeleeElite,
                    faction.MilitiaRanged,
                    faction.MilitiaRangedElite,
                ];
            }
            catch
            {
                return [];
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Rename(WCharacter troop, string newName)
        {
            Log.Debug($"Renaming troop {troop?.Name} to '{newName}'.");

            troop.Name = newName.Trim();
        }

        public static void ChangeGender(WCharacter troop)
        {
            Log.Debug($"Changing gender for troop {troop?.Name}.");

            troop.IsFemale = !troop.IsFemale;
        }

        public static void ModifySkill(WCharacter troop, SkillObject skill, int delta)
        {
            Log.Debug(
                $"Modifying skill {skill?.Name} for troop {troop?.Name} by {delta} (current: {troop?.GetSkill(skill)})."
            );

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
                // Respect existing "CanDecrement" constraints upstream
                int newValue = current - 1;
                if (newValue < 0)
                    return;

                // Refund the cost of the point (i.e., the cost that was paid to go from newValue -> newValue + 1)
                int refund = TroopRules.SkillPointXpCost(newValue);
                troop.SetSkill(skill, newValue);
                TroopXpService.Refund(troop, refund);
            }
        }

        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            Log.Debug($"Adding upgrade target '{targetName}' to troop {troop?.Name}.");

            // Determine the position in the tree
            List<int> path = [.. troop.PositionInTree, troop.UpgradeTargets.Length];

            // Wrap the custom troop
            var child = new WCharacter(
                troop.Faction?.StringId == Player.Kingdom?.StringId,
                troop.IsElite,
                path: path
            );

            // Copy from the original troop
            child.FillFrom(troop, keepUpgrades: false, keepEquipment: false, keepSkills: true);

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
            Log.Debug($"Removing troop {troop?.Name}.");

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
            Log.Debug($"Ranking up troop {troop?.Name} (current level: {troop?.Level}).");

            if (troop == null || troop.IsMaxTier)
                return;

            int cost = TroopRules.RankUpCost(troop);
            if (Player.Gold < cost)
                return;

            // check XP first
            if (!TroopXpService.TrySpend(troop, cost))
                return;

            // now charge gold and upgrade
            Player.ChangeGold(-cost);
            troop.Level += 5;
        }

        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            Log.Debug($"Getting max convertible from {from?.Name} to {to?.Name}.");

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
            Log.Debug(
                $"Converting {amountRequested} of {from?.Name} to {to?.Name} at cost {cost}."
            );

            // Clamp to max possible
            int max = GetMaxConvertible(from, to);
            int amount = Math.Min(amountRequested, max);

            // Apply cost
            if (Player.Gold < cost)
                return;
            Player.ChangeGold(-cost);

            // Mutate roster
            Player.Party.MemberRoster.RemoveTroop(from, amount);
            Player.Party.MemberRoster.AddTroop(to, amount);
        }

        public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
        {
            var sources = new List<WCharacter>(2);

            if (retinue is null || !retinue.IsRetinue)
            {
                Log.Error($"RetinueSources: {retinue?.StringId} is not a retinue.");
                return sources;
            }

            // Identify which root to look under for culture and faction
            WCharacter cultureRoot = null,
                factionRoot = null;

            if (retinue.IsElite)
            {
                cultureRoot = retinue.Culture?.RootElite;
                factionRoot = retinue.Faction?.RootElite;
            }
            else
            {
                cultureRoot = retinue.Culture?.RootBasic;
                factionRoot = retinue.Faction?.RootBasic;
            }

            // Culture pick
            var culturePick = TroopMatcher.PickBestFromTree(cultureRoot, retinue);
            if (culturePick.IsValid)
                sources.Add(culturePick);

            // Faction pick (avoid duplicate)
            var factionPick = TroopMatcher.PickBestFromTree(
                factionRoot,
                retinue,
                exclude: culturePick
            );
            if (factionPick.IsValid)
                sources.Add(factionPick);

            return sources;
        }
    }
}
