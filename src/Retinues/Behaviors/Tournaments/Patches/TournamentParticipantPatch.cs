using System.Collections.Generic;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Tournaments.Patches
{
    /// <summary>
    /// Keeps custom troops out of tournament brackets.
    ///
    /// The vanilla tournament fill (FightTournamentGame) adds non-hero participants from the host
    /// settlement's garrison — any troop of tier 3+ qualifies via CanBeAParticipant. Once the
    /// player garrisons custom troops (e.g. House Guard retinues), those flood the bracket and
    /// crowd out lords. Custom troops are the player's personal/clan units and should never be
    /// arena fodder, so we exclude them from participant selection.
    /// </summary>
    internal static class TournamentParticipantPatch
    {
        /// <summary>
        /// Prevent custom non-hero troops from qualifying as tournament participants. This blocks
        /// the garrison fill at the source, so the bracket fills with vanilla troops instead and
        /// keeps a full participant count.
        /// </summary>
        [HarmonyPatch(typeof(FightTournamentGame), "CanBeAParticipant")]
        internal static class FightTournamentGame_CanBeAParticipant
        {
            [SafeMethod]
            private static bool Prefix(CharacterObject character, ref bool __result)
            {
                if (!Configuration.ExcludeCustomTroopsFromTournaments)
                    return true; // feature disabled; run the original

                if (character != null && !character.IsHero && WCharacter.Get(character).IsCustom)
                {
                    __result = false;
                    return false; // skip the original; custom troops cannot participate
                }

                return true; // run the original for everyone else
            }
        }

        /// <summary>
        /// Safety net: strip any custom non-hero troop that still slipped into the participant
        /// list through another fill path. Heroes (lords, the player) are always kept.
        ///
        /// CRITICAL: the participant count must never shrink. TournamentBehavior.CreateParticipants
        /// copies this list into a fixed array of MaximumParticipantCount slots; vanilla always
        /// tops the list up to exactly that count, so any slot we leave unfilled stays null and
        /// crashes TournamentMatch.AddParticipant (NullReferenceException) when the bracket is
        /// built. That happens when another fill path (e.g. an arena overhaul mod) bypasses
        /// CanBeAParticipant, so customs get in — and this postfix then removed them without
        /// replacement. After stripping, the list is topped back up with the settlement culture's
        /// own troop tree (the same fallback vanilla uses); if no replacement can be found, the
        /// customs are put back — a custom troop in the bracket is better than a crash.
        /// </summary>
        [HarmonyPatch(typeof(FightTournamentGame), "GetParticipantCharacters")]
        internal static class FightTournamentGame_GetParticipantCharacters
        {
            [SafeMethod]
            private static void Postfix(Settlement settlement, MBList<CharacterObject> __result)
            {
                if (!Configuration.ExcludeCustomTroopsFromTournaments)
                    return; // feature disabled

                if (__result == null || __result.Count == 0)
                    return;

                var removed = new List<CharacterObject>();

                for (int i = __result.Count - 1; i >= 0; i--)
                {
                    var c = __result[i];
                    if (c != null && !c.IsHero && WCharacter.Get(c).IsCustom)
                    {
                        removed.Add(c);
                        __result.RemoveAt(i);
                    }
                }

                if (removed.Count == 0)
                    return; // nothing stripped, count untouched

                Refill(settlement, __result, removed);
            }

            /// <summary>
            /// Tops the participant list back up to its original count from the settlement
            /// culture's basic/elite troop trees, falling back to the stripped customs.
            /// </summary>
            internal static void Refill(
                Settlement settlement,
                MBList<CharacterObject> result,
                List<CharacterObject> removed
            )
            {
                int target = result.Count + removed.Count;

                var culture =
                    settlement?.Culture
                    ?? TaleWorlds.Core.Game.Current?.ObjectManager?.GetObject<CultureObject>(
                        "empire"
                    );

                var pool = new List<CharacterObject>();
                CollectUpgradeTree(culture?.BasicTroop, pool);
                CollectUpgradeTree(culture?.EliteBasicTroop, pool);

                // Prefer arena-appropriate tiers (3-5, like vanilla), then anything else.
                foreach (var preferredOnly in new[] { true, false })
                {
                    for (int i = 0; i < pool.Count && result.Count < target; i++)
                    {
                        var c = pool[i];
                        if (c == null || c.IsHero)
                            continue;
                        if (preferredOnly && (c.Tier < 3 || c.Tier > 5))
                            continue;
                        if (result.Contains(c))
                            continue;
                        if (WCharacter.Get(c).IsCustom)
                            continue;

                        result.Add(c);
                    }

                    if (result.Count >= target)
                        break;
                }

                // Last resort: never hand the tournament a short list. Put the customs back
                // rather than leave null bracket slots that crash the mission.
                for (int i = 0; i < removed.Count && result.Count < target; i++)
                {
                    Log.Warning(
                        $"Tournament fill: no vanilla replacement found, keeping custom troop "
                            + $"'{removed[i].StringId}' in the bracket to avoid a short list."
                    );
                    result.Add(removed[i]);
                }
            }

            /// <summary>
            /// Collects a troop and its whole upgrade tree into the pool (deduped).
            /// </summary>
            private static void CollectUpgradeTree(CharacterObject root, List<CharacterObject> into)
            {
                if (root == null || into == null || into.Contains(root))
                    return;

                into.Add(root);

                var targets = root.UpgradeTargets;
                if (targets == null)
                    return;

                foreach (var t in targets)
                    CollectUpgradeTree(t, into);
            }
        }
    }
}
