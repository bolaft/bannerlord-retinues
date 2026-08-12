using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Behaviors.Retinues.Patches
{
    /// <summary>
    /// Keeps retinue conversion player-only at the structural level: strips retinue-typed
    /// entries from the AI party upgrader's choices for any party other than the player's.
    /// Retinue upgrade targets are injected for the player's party-screen session and restored
    /// when it closes, so the AI upgrader (which runs on campaign ticks, while no screen is
    /// open) should never see one — this guard turns that timing property into a guarantee,
    /// and also covers stale retinue targets carried by saves from older versions.
    /// </summary>
    [HarmonyPatch(typeof(PartyUpgraderCampaignBehavior), "GetPossibleUpgradeTargets")]
    internal static class RetinueAIUpgradeFilterPatch
    {
        // Field of the private nested TroopUpgradeArgs struct; same name on BL 1.2/1.3/1.4.
        private static FieldInfo _upgradeTargetField;

        [HarmonyPostfix]
        private static void Postfix(PartyBase party, object __result)
        {
            try
            {
                if (__result is IList list)
                    FilterForParty(party, list);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Retinue AI upgrade filter failed.");
            }
        }

        /// <summary>
        /// Removes retinue-typed entries from the upgrade choice list unless the party is the
        /// player's. Internal so the test suite can exercise the filter directly.
        /// </summary>
        internal static void FilterForParty(PartyBase party, IList list)
        {
            if (list == null || list.Count == 0)
                return;

            if (party == PartyBase.MainParty)
                return; // The player may convert eligible troops into retinues.

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var entry = list[i];
                if (entry == null)
                    continue;

                _upgradeTargetField ??= AccessTools.Field(entry.GetType(), "UpgradeTarget");

                if (_upgradeTargetField?.GetValue(entry) is not CharacterObject target)
                    continue;

                var wc = WCharacter.Get(target);
                if (wc?.IsRetinue == true)
                    list.RemoveAt(i);
            }
        }
    }
}
