using System;
using HarmonyLib;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Behaviors.Doctrines.Feats.Loot;
using Retinues.Safety.Uninstall;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Behaviors.Doctrines.Patches
{
    [HarmonyPatch(typeof(SkillLevelingManager), nameof(SkillLevelingManager.OnPrisonBreakEnd))]
    internal static class PrisonBreakFeatPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Hero prisonerHero, bool isSucceeded)
        {
            try
            {
                // The dedicated prison-break result excludes ransoms and random escapes.
                if (!isSucceeded || UninstallBehavior.IsPrepared)
                    return;

                var feat = Feat.Get(FeatCatalog.PR_RescueMission.Id);
                if (feat?.IsInProgress == true && Feat_PragmaticScavengers_RescueMission.IsAlliedLord(prisonerHero))
                    feat.Add();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Could not record Rescue Mission prison-break progress.");
            }
        }
    }
}
