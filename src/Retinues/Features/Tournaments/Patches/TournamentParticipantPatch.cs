using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace Retinues.Features.Tournaments.Patches
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
                if (!Config.ExcludeCustomTroopsFromTournaments)
                    return true; // feature disabled; run the original

                if (character != null && !character.IsHero && new WCharacter(character).IsCustom)
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
        /// built. That is exactly what happened when another tournament mod (e.g. RBM) filled the
        /// roster through its own path — bypassing CanBeAParticipant, so customs got in — and this
        /// postfix then removed them without replacement. After stripping, the list is topped back
        /// up with the settlement culture's own troop tree (the same fallback vanilla uses); if no
        /// replacement can be found, the customs are put back — a custom troop in the bracket is
        /// better than a crash.
        /// </summary>
        [HarmonyPatch(typeof(FightTournamentGame), "GetParticipantCharacters")]
        internal static class FightTournamentGame_GetParticipantCharacters
        {
            [SafeMethod]
            private static void Postfix(
                FightTournamentGame __instance,
                TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
                MBList<CharacterObject> __result
            )
            {
                if (!Config.ExcludeCustomTroopsFromTournaments)
                    return; // feature disabled

                if (__result == null || __result.Count == 0)
                    return;

                var removed = new System.Collections.Generic.List<CharacterObject>();

                for (int i = __result.Count - 1; i >= 0; i--)
                {
                    var c = __result[i];
                    if (c != null && !c.IsHero && new WCharacter(c).IsCustom)
                    {
                        removed.Add(c);
                        __result.RemoveAt(i);
                    }
                }

                if (removed.Count == 0)
                    return; // nothing stripped, count untouched

                // Refill to the original count from the settlement culture's basic troop tree —
                // the same source vanilla's own top-up uses. Skip anything already listed,
                // heroes, and customs.
                int target = __result.Count + removed.Count;

                var culture =
                    settlement?.Culture
                    ?? TaleWorlds.Core.Game.Current?.ObjectManager?.GetObject<CultureObject>(
                        "empire"
                    );

                var pool = new System.Collections.Generic.List<CharacterObject>();
                CollectUpgradeTree(culture?.BasicTroop, pool);
                CollectUpgradeTree(culture?.EliteBasicTroop, pool);

                // Prefer arena-appropriate tiers (3-5, like vanilla), then anything else.
                foreach (var preferredOnly in new[] { true, false })
                {
                    for (int i = 0; i < pool.Count && __result.Count < target; i++)
                    {
                        var c = pool[i];
                        if (c == null || c.IsHero)
                            continue;
                        if (preferredOnly && (c.Tier < 3 || c.Tier > 5))
                            continue;
                        if (__result.Contains(c))
                            continue;
                        if (new WCharacter(c).IsCustom)
                            continue;

                        __result.Add(c);
                    }

                    if (__result.Count >= target)
                        break;
                }

                // Last resort: never hand the tournament a short list. Put the customs back
                // rather than leave null bracket slots that crash the mission.
                for (int i = 0; i < removed.Count && __result.Count < target; i++)
                {
                    Log.Warn(
                        $"Tournament fill: no vanilla replacement found, keeping custom troop "
                            + $"'{removed[i].StringId}' in the bracket to avoid a short list."
                    );
                    __result.Add(removed[i]);
                }
            }

            /// <summary>
            /// Collects a troop and its whole upgrade tree into the pool (breadth-first, deduped).
            /// </summary>
            private static void CollectUpgradeTree(
                CharacterObject root,
                System.Collections.Generic.List<CharacterObject> into
            )
            {
                if (root == null || into == null)
                    return;

                var queue = new System.Collections.Generic.Queue<CharacterObject>();
                queue.Enqueue(root);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    if (current == null || into.Contains(current))
                        continue;

                    into.Add(current);

                    var targets = current.UpgradeTargets;
                    if (targets == null)
                        continue;

                    for (int i = 0; i < targets.Length; i++)
                        if (targets[i] != null)
                            queue.Enqueue(targets[i]);
                }
            }
        }
    }
}
