using System;
using System.Reflection;
using HarmonyLib;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace Retinues.Core.Features.Xp.Patches
{
    [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.CommitXpGain))]
    internal static class BattleXpTracker
    {
        private static readonly FieldInfo _fiRoster = AccessTools.Field(
            typeof(MapEventParty),
            "_roster"
        );

        private static readonly Type _fteType = AccessTools.TypeByName(
            "TaleWorlds.CampaignSystem.Roster.FlattenedTroopRosterElement"
        );

        private static readonly PropertyInfo _fteTroop =
            _fteType != null ? AccessTools.Property(_fteType, "Troop") : null;
        private static readonly PropertyInfo _fteKilled =
            _fteType != null ? AccessTools.Property(_fteType, "IsKilled") : null;

        public const float xpMultiplier = 0.02f; // 2% of vanilla per-troop award
        public const float xpMultiplierNonMain = 0.25f; // 25% for non-main parties

        static void Postfix(MapEventParty __instance)
        {
            try
            {
                var mobile = __instance?.Party?.MobileParty;
                if (mobile == null)
                    return;

                var party = new WParty(mobile);
                if (party.PlayerFaction == null)
                    return; // non-player faction, skip

                // Pull the flattened roster like vanilla does
                var flattened = _fiRoster?.GetValue(__instance);
                if (flattened == null || _fteType == null)
                    return;

                var model = Campaign.Current?.Models?.PartyTrainingModel;
                if (model == null)
                    return;

                int processed = 0,
                    awarded = 0;
                foreach (var row in (System.Collections.IEnumerable)flattened)
                {
                    processed++;
                    var co = _fteTroop?.GetValue(row) as CharacterObject;
                    if (co == null)
                        continue;

                    var troop = new WCharacter(co);
                    if (!troop.IsValid || !troop.IsCustom)
                        continue; // skip non-custom troops

                    if (_fteKilled != null && _fteKilled.GetValue(row) is bool killed && killed)
                        continue; // skip killed lines

                    // Compute the same numbers vanilla uses
                    var explained = model.CalculateXpGainFromBattles(
                        (FlattenedTroopRosterElement)row,
                        party.Base.Party
                    );
                    int baseXp = MathF.Round(explained.ResultNumber);
                    if (baseXp <= 0)
                        continue; // no XP to give

                    int shared = model.GenerateSharedXp(troop.Base, baseXp, mobile);
                    int award = Math.Max(0, baseXp - shared);
                    if (award <= 0)
                        continue; // no XP to give

                    // Apply multiplier
                    int gain = (int)(award * xpMultiplier);

                    // Reduce for non-main parties
                    if (!mobile.IsMainParty)
                        gain = (int)(gain * xpMultiplierNonMain);

                    if (gain <= 0)
                        continue; // no XP to give after multiplier

                    TroopXpBehavior.Add(troop, gain);
                    awarded++;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
