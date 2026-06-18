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
        /// </summary>
        [HarmonyPatch(typeof(FightTournamentGame), "GetParticipantCharacters")]
        internal static class FightTournamentGame_GetParticipantCharacters
        {
            [SafeMethod]
            private static void Postfix(MBList<CharacterObject> __result)
            {
                if (!Config.ExcludeCustomTroopsFromTournaments)
                    return; // feature disabled

                if (__result == null || __result.Count == 0)
                    return;

                __result.RemoveAll(c =>
                    c != null && !c.IsHero && new WCharacter(c).IsCustom
                );
            }
        }
    }
}
