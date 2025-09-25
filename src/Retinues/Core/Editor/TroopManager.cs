using System;
using System.Collections.Generic;
using Retinues.Core.Features.Xp;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       All Troops                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Rename(WCharacter troop, string newName)
        {
            Log.Debug($"Renaming troop {troop?.Name} to '{newName}'.");

            try
            {
                troop.Name = newName.Trim();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static void ChangeGender(WCharacter troop)
        {
            Log.Debug($"Changing gender for troop {troop?.Name}.");
            try
            {
                troop.IsFemale = !troop.IsFemale;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static void ModifySkill(WCharacter troop, SkillObject skill, int delta)
        {
            Log.Debug(
                $"Modifying skill {skill?.Name} for troop {troop?.Name} by {delta} (current: {troop?.GetSkill(skill)})."
            );

            try
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
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static WCharacter AddUpgradeTarget(WCharacter troop, string targetName)
        {
            Log.Debug($"Adding upgrade target '{targetName}' to troop {troop?.Name}.");

            try
            {
                // Determine the position in the tree
                List<int> path = [.. troop.PositionInTree, troop.UpgradeTargets.Length];

                // Wrap the custom troop
                var child = new WCharacter(
                    troop.Faction?.StringId == Player.Kingdom?.StringId,
                    troop.IsElite,
                    false,
                    path
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
            catch (Exception e)
            {
                Log.Exception(e);
                return null;
            }
        }

        public static void Remove(WCharacter troop)
        {
            Log.Debug($"Removing troop {troop?.Name}.");

            try
            {
                // Stock the troop's equipment
                foreach (var item in troop.Equipment.Items)
                    item.Stock();

                // Remove the troop
                troop.Remove();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Retinues                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void RankUp(WCharacter troop)
        {
            Log.Debug($"Ranking up troop {troop?.Name} (current level: {troop?.Level}).");

            try
            {
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
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static List<WCharacter> GetRetinueSourceTroops(WCharacter retinue)
        {
            Log.Debug($"Getting source troops for retinue {retinue?.Name}.");

            List<WCharacter> sources = [];

            try
            {
                WCharacter cultureRoot;
                WCharacter factionRoot;

                if (retinue.StringId == retinue.Faction?.RetinueElite.StringId)
                {
                    cultureRoot = retinue.Culture?.RootElite;
                    factionRoot = retinue.Faction?.RootElite;
                }
                else if (retinue.StringId == retinue.Faction?.RetinueBasic.StringId)
                {
                    cultureRoot = retinue.Culture?.RootBasic;
                    factionRoot = retinue.Faction?.RootBasic;
                }
                else
                {
                    Log.Warn($"Troop {retinue.StringId} is not a retinue troop");
                    return sources;
                }

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
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            return sources;
        }

        private static bool IsEligibleForRetinue(WCharacter troop, WCharacter retinue)
        {
            Log.Debug($"Checking if troop {troop?.Name} is eligible for retinue {retinue?.Name}.");

            try
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
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }

        public static int GetMaxConvertible(WCharacter from, WCharacter to)
        {
            Log.Debug($"Getting max convertible from {from?.Name} to {to?.Name}.");

            try
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
            catch (Exception e)
            {
                Log.Exception(e);
                return 0;
            }
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

            try
            {
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
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
