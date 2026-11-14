using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety.Uninstall
{
    internal static class UninstallCleanup
    {
        internal static void Run(bool removeOnly)
        {
            if (Campaign.Current == null) throw new InvalidOperationException("Load a campaign first.");
            if (UninstallBehavior.HasFailed)
                throw new InvalidOperationException("Reload the pre-uninstall backup before retrying cleanup.");
            if (Campaign.Current.SaveHandler?.IsSaving == true)
                throw new InvalidOperationException("Wait for the current save to finish before uninstalling.");
            if (MobileParty.MainParty?.MapEvent != null)
                throw new InvalidOperationException("Finish the current encounter before uninstalling.");
            UninstallPersistenceGuard.EnsureInstalled();
            UninstallSaveGuard.EnsureInstalled();
            UninstallHooks.RestoreTemporaryReferences();
            var replacement = CreateResolver();
            var plan = Scan(replacement, removeOnly);
            var behavior = Campaign.Current.GetCampaignBehavior<UninstallBehavior>()
                ?? throw new InvalidOperationException("The uninstall behavior is unavailable; restart the game before trying again.");
            try
            {
                plan.Apply();
                var check = Scan(replacement, removeOnly);
                if (check.Changes != 0) throw new InvalidOperationException("Custom troop references remain after cleanup.");
                behavior.Prepare();
            }
            catch
            {
                // A reflected setter from another mod may fail during application. Do not
                // allow a partially applied cleanup to overwrite a recoverable save.
                behavior.CleanupFailed = true;
                throw;
            }
            UninstallHooks.Log($"[Uninstall] Verified cleanup complete: {plan.Changes} reference/roster changes. Save to a new slot and exit.");
        }

        internal static Func<CharacterObject, CharacterObject> CreateResolver()
        {
            var troops = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>()
                .Where(t => t != null && !t.IsHero && !UninstallReferencePlan.IsCustom(t) && t.Occupation == Occupation.Soldier)
                .ToList();
            var cache = new Dictionary<string, CharacterObject>(StringComparer.Ordinal);
            return source =>
            {
                if (cache.TryGetValue(source.StringId, out var existing)) return existing;
                // Keep overhaul troops when their module remains installed. No dependency on Retinues wrappers.
                var candidates = troops.Where(t => t.Culture == source.Culture).ToList();
                if (candidates.Count == 0) candidates = troops;
                var result = candidates.OrderBy(t => Math.Abs(t.Tier - source.Tier))
                    .ThenBy(t => t.IsMounted == source.IsMounted ? 0 : 1)
                    .ThenBy(t => t.IsRanged == source.IsRanged ? 0 : 1)
                    .ThenBy(t => t.StringId, StringComparer.Ordinal).FirstOrDefault();
                if (result == null) throw new InvalidOperationException("No replacement troops are available.");
                cache[source.StringId] = result;
                return result;
            };
        }

        private static UninstallReferencePlan Scan(Func<CharacterObject, CharacterObject> resolve, bool removeOnly, bool includeBehaviorSnapshots = false)
        {
            var plan = new UninstallReferencePlan(resolve, removeOnly, includeBehaviorSnapshots);
            plan.Visit(TaleWorlds.Core.Game.Current, "Game");
            plan.Visit(Campaign.Current, "Campaign");
            foreach (var party in MobileParty.All) plan.Visit(party.Party, "Party:" + party.StringId);
            foreach (var settlement in Settlement.All) plan.Visit(settlement, "Settlement:" + settlement.StringId);
            foreach (var hero in Hero.AllAliveHeroes) plan.Visit(hero, "Hero:" + hero.StringId);
            foreach (var hero in Hero.DeadOrDisabledHeroes) plan.Visit(hero, "Hero:" + hero.StringId);
            foreach (var clan in Clan.All) plan.Visit(clan, "Clan:" + clan.StringId);
            foreach (var behavior in Campaign.Current.CampaignBehaviorManager.GetBehaviors<CampaignBehaviorBase>())
                if (behavior.GetType().Assembly != typeof(UninstallCleanup).Assembly) plan.VisitBehavior(behavior);

            // Upgrade targets come from XML and lack Saveable attributes. Retinues can temporarily
            // inject targets while the party screen is open, so check these separately as well.
            foreach (var troop in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
            {
                if (UninstallReferencePlan.IsCustom(troop)) continue;
                plan.Visit(troop, "Troop:" + troop.StringId);
                plan.PlanUpgradeTargets(troop);
            }
            foreach (var template in MBObjectManager.Instance.GetObjectTypeList<PartyTemplateObject>())
                plan.Visit(template.Stacks, "Template:" + template.StringId);
            return plan;
        }

        internal static void VerifyPreparedSave(bool includeBehaviorSnapshots)
        {
            if (Scan(CreateResolver(), false, includeBehaviorSnapshots).Changes != 0)
                throw new InvalidOperationException("Custom troop references returned after cleanup. Reload the pre-uninstall backup and report the log.");
        }
    }
}
