using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Retinues.Core.Features.Xp.Patches
{
    /// <summary>
    /// Captures computed XP per troop and publishes it.
    /// </summary>
    [HarmonyPatch(typeof(MobilePartyTrainingBehavior), "OnDailyTickParty")]
    internal static class MobilePartyTrainingBehavior_OnDailyTickParty_Patch
    {
        public const float XpMultiplier = 0.2f; // 20% of the original XP
        public const float XpMultiplierNonMain = 0.25f; // 25% of XP for non-main parties

        // Compute everything upfront (same model call + rounding) and cache for the Postfix.
        private static void Prefix(MobileParty mobileParty)
        {
            try
            {
                var party = new WParty(mobileParty);
                var xpTotals = new Dictionary<WCharacter, int>();

                if (party == null || party.MemberRoster == null)
                    return; // no party or no troops

                if (party.PlayerFaction == null)
                    return; // not a player faction party

                // Mirror vanilla enumeration order and math.
                foreach (var element in party.MemberRoster.Elements)
                {
                    if (!element.Troop.IsCustom)
                        continue; // only care about custom troops

                    // Vanilla uses PartyTrainingModel.GetEffectiveDailyExperience(...) per element.
                    ExplainedNumber en =
                        Campaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(
                            mobileParty,
                            element.Base
                        );
                    int each = MathF.Round(en.ResultNumber);
                    int total = each * element.Number;

                    if (total <= 0)
                        continue; // no XP to give

                    // Normalize XP gain
                    var gain = (int)(total * XpMultiplier);

                    // Reduce XP for non-main parties
                    if (party.IsMainParty == false)
                        gain = (int)(gain * XpMultiplierNonMain);

                    // Track total XP per troop
                    if (!xpTotals.ContainsKey(element.Troop))
                        xpTotals[element.Troop] = 0;
                    xpTotals[element.Troop] += gain;
                }

                // Publish XP gain
                foreach (var kvp in xpTotals)
                {
                    Log.Debug($"Computed training XP for {kvp.Key.Name}: {kvp.Value} XP");
                    TroopXpBehavior.Add(kvp.Key, kvp.Value);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "MobilePartyTrainingBehavior_OnDailyTickParty_Patch Prefix");
            }
        }
    }
}
