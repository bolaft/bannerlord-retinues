using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using Retinues.Game.Wrappers;
using Retinues.Features.Xp.Behaviors;
using Retinues.Utils;

namespace Retinues.Core.Features.Xp.Patches
{
    /// <summary>
    /// Captures computed XP per troop and publishes it.
    /// </summary>
    [HarmonyPatch(typeof(MobilePartyTrainingBehavior), "OnDailyTickParty")]
    internal static class MobilePartyTrainingBehavior_OnDailyTickParty_Patch
    {
        public const float xpMultiplier = 0.1f; // 10% of the original XP
        public const float xpMultiplierNonMain = 0.25f; // 25% of XP for non-main parties

        // Compute everything upfront (same model call + rounding) and cache for the Postfix.
        private static void Prefix(MobileParty mobileParty)
        {
            try
            {
                var party = new WParty(mobileParty);

                // Mirror vanilla enumeration order and math.
                foreach (var element in party.MemberRoster.Elements)
                {
                    if (!element.Troop.IsCustom)
                        continue; // only care about custom troops

                    // Vanilla uses PartyTrainingModel.GetEffectiveDailyExperience(...) per element.
                    ExplainedNumber en = Campaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(mobileParty, element.Base);
                    int each = MathF.Round(en.ResultNumber);
                    int total = each * element.Number;

                    // Normalize XP gain
                    var gain = (int)(total * xpMultiplier);

                    // Reduce XP for non-main parties
                    if (party.IsMainParty == false)
                        gain = (int)(gain * xpMultiplierNonMain);

                    // Publish to the XP behavior
                    TroopXpBehavior.Add(element.Troop, gain);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "MobilePartyTrainingBehavior_OnDailyTickParty_Patch Prefix");
            }
        }
    }
}
