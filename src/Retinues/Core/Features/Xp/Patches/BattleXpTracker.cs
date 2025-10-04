using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Core.Features.Xp.Patches
{
    [HarmonyPatch(typeof(MapEventParty), nameof(MapEventParty.CommitXpGain))]
    internal static class BattleXpTracker
    {
        [ThreadStatic]
        internal static bool InCommitXpGain;

        [ThreadStatic]
        private static Dictionary<WCharacter, int> _xpBefore;

        public const float xpMultiplier = 0.02f; // 2% of the original XP
        public const float xpMultiplierNonMain = 0.25f; // 25% of XP for non-main parties

        static void Prefix(MapEventParty __instance)
        {
            try
            {
                InCommitXpGain = true;

                var partyBase = __instance?.Party;
                var mobile = partyBase?.MobileParty;
                if (mobile == null)
                    return;

                var party = new WParty(mobile);

                var roster = party.MemberRoster;
                if (roster == null)
                    return;

                var snap = _xpBefore ??= new Dictionary<WCharacter, int>(128);
                snap.Clear();

                // Snapshot current XP for each roster line (non-heroes are the ones that get troop XP)
                foreach (var e in roster.Elements)
                {
                    if (!e.Troop.IsCustom)
                        continue;
                    snap[e.Troop] = e.Xp;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        static void Postfix(MapEventParty __instance)
        {
            try
            {
                if (_xpBefore == null)
                    return;

                var mobile = __instance?.Party?.MobileParty;
                if (mobile == null)
                    return;

                var party = new WParty(mobile);

                // Compute net diffs after vanilla finished applying all internal AddXpToTroop calls
                foreach (var e in party.MemberRoster.Elements)
                {
                    if (!e.Troop.IsCustom)
                        continue;

                    int before = _xpBefore.TryGetValue(e.Troop, out var b) ? b : 0;
                    var roster = party.MemberRoster.Base;
                    int after = e.Xp;
                    int delta = after - before;

                    // Normalize XP gain
                    delta = (int)(delta * xpMultiplier);

                    // Reduce XP for non-main parties
                    if (party.IsMainParty == false)
                        delta = (int)(delta * xpMultiplierNonMain);

                    if (delta <= 0)
                        continue;

                    Log.Debug(
                        $"+{delta} XP to {e.Troop.StringId} ({(party.IsMainParty ? "main" : "non-main")} party)."
                    );
                    TroopXpBehavior.Add(e.Troop, delta);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally
            {
                InCommitXpGain = false;
                _xpBefore?.Clear();
            }
        }
    }
}
