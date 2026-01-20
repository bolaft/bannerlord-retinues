using HarmonyLib;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Behaviors.Troops.Patches
{
    /// <summary>
    /// Patches for the remaining vanilla "injection points" where militia/caravan/villager troops
    /// are explicitly added to rosters (quests, rebellions, etc).
    /// Uses WParty.SwapTroops which relies on CharacterMatcher.FindFactionCounterpart.
    /// </summary>
    [SafeClass]
    internal static class TroopSwapHolePatches
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Rebellion garrison                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [HarmonyPatch(typeof(RebellionsCampaignBehavior), "ApplyRebellionConsequencesToSettlement")]
        private static class Rebellion_GarrisonMilitia_Postfix
        {
            private static void Postfix(Settlement settlement)
            {
                try
                {
                    var faction = WSettlement.Get(settlement)?.GetBaseTroopsFaction();
                    if (faction == null)
                        return;

                    var garrison = settlement?.Town?.GarrisonParty;
                    if (garrison == null)
                        return;

                    var party = WParty.Get(garrison);
                    party?.SwapTroops(faction, filter: t => t.IsMilitia);

                    Log.Debug(
                        $"Rebellion_GarrisonMilitia_Postfix: swapped garrison troops for settlement {settlement.Name}."
                    );
                }
                catch (System.Exception ex)
                {
                    Log.Exception(ex, "Rebellion garrison swap failed");
                }
            }
        }
    }
}
