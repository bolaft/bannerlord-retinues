using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Safety.Patches
{
    [HarmonyPatch]
    internal static class GetMoraleEffectsFromSkill_SafePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(DefaultPartyMoraleModel),
                "GetMoraleEffectsFromSkill",
                [typeof(MobileParty), typeof(ExplainedNumber).MakeByRefType()]
            );
        }

        [HarmonyPrefix]
        private static bool Prefix(MobileParty party, ref ExplainedNumber bonus)
        {
            try
            {
                if (party == null || party.Party == null)
                {
                    Log.Error($"GetMoraleEffectsFromSkill: party/party.Party is null.");
                    return false; // skip original
                }

                CharacterObject leader = null;
                try
                {
                    leader = SkillHelper.GetEffectivePartyLeaderForSkill(party.Party);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    return false; // skip original
                }

                if (leader == null)
                    return false;

                int leadership = 0;
                try
                {
                    leadership = leader.GetSkillValue(DefaultSkills.Leadership);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                    return false; // skip original
                }

                if (leadership > 0)
                {
                    try
                    {
#if BL13
                        SkillHelper.AddSkillBonusForCharacter(
                            DefaultSkillEffects.LeadershipMoraleBonus,
                            leader,
                            ref bonus
                        );
#else
                        SkillHelper.AddSkillBonusForCharacter(
                            DefaultSkills.Leadership,
                            DefaultSkillEffects.LeadershipMoraleBonus,
                            leader,
                            ref bonus
                        );
#endif
                    }
                    catch (Exception e)
                    {
                        // swallow, still skip original
                        Log.Exception(e);
                    }
                }

                return false; // handled safely, don't run original
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false; // skip original on any unexpected issue
            }
        }
    }

    /// <summary>
    /// Harmony patch for DefaultPartyMoraleModel.GetEffectivePartyMorale.
    /// Adds a fail-safe: if an exception occurs, sets morale to 50 with a Retinues marker.
    /// </summary>
    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale)
    )]
    internal static class GetEffectivePartyMorale_FailSafe
    {
        [HarmonyFinalizer]
        private static void Finalizer(
            DefaultPartyMoraleModel __instance,
            MobileParty mobileParty,
            bool includeDescription,
            ref ExplainedNumber __result,
            Exception __exception
        )
        {
            if (__exception == null)
                return;

            try
            {
                Log.Exception(__exception);

                __result = new ExplainedNumber(50f, includeDescription);
                __result.Add(0f, new TextObject("Retinues: fail-safe morale"));
            }
            catch (Exception)
            {
                __result = new ExplainedNumber(50f, includeDescription);
                Log.Exception(__exception, "Failed to apply fail-safe morale");
            }
        }
    }
}
