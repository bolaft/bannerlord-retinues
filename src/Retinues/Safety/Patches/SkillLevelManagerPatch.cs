using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Safety.Patches
{
    /// <summary>
    /// Prevent rare NREs in vanilla DefaultSkillLevelingManager when recruiter hero or HeroDeveloper is null.
    /// </summary>
    [HarmonyPatch(
        typeof(DefaultSkillLevelingManager),
        nameof(DefaultSkillLevelingManager.OnTroopRecruited)
    )]
    internal static class DefaultSkillLevelingManagerGuards
    {
        private static readonly HashSet<string> _reported = new(StringComparer.Ordinal);

        [HarmonyPrefix]
        private static bool Prefix(Hero hero, int amount, int tier)
        {
            try
            {
                if (hero == null)
                {
                    // Vanilla can crash here; just skip skill XP.
                    return false;
                }

                if (hero.HeroDeveloper == null)
                {
                    var key = hero.StringId ?? hero.Name?.ToString() ?? "<unknown-hero>";
                    if (_reported.Add(key))
                    {
                        Log.Error(
                            $"DefaultSkillLevelingManagerGuards: HeroDeveloper is null for hero '{key}'. "
                                + $"Skipping OnTroopRecruited XP (amount={amount}, tier={tier})."
                        );
                    }

                    return false;
                }

                return true; // run vanilla
            }
            catch (Exception e)
            {
                Log.Exception(e, "DefaultSkillLevelingManagerGuards: Exception in Prefix.");
                return true; // ignore and run vanilla
            }
        }
    }
}
