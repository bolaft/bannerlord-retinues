using System.Collections.Generic;
using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Domain.Characters.Helpers
{
    /// <summary>
    /// Helpers for selecting a representative WCharacter string ID to use as the 3D model
    /// preview on the doctrine selection screen.
    ///
    /// Preference order for all methods:
    ///   highest-tier troop in the relevant tree > first troop > null (falls back to player hero)
    /// </summary>
    public static class DoctrinePreviewHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Highest-tier troop in the player clan's retinue trees.
        /// Falls back to <see cref="PlayerClanElite"/> if no retinues exist.
        /// </summary>
        public static string PlayerRetinue() =>
            HighestTier(Player.Clan?.RosterRetinues) ?? PlayerClanElite();

        /// <summary>
        /// Highest-tier troop in the player clan's basic (non-elite) tree.
        /// Falls back to <see cref="PlayerClanElite"/> if the tree is empty.
        /// </summary>
        public static string PlayerClanBasic() =>
            HighestTier(Player.Clan?.RosterBasic) ?? PlayerClanElite();

        /// <summary>
        /// Highest-tier troop in the player clan's elite tree.
        /// Falls back to <see cref="PlayerClanBasic"/> if the tree is empty.
        /// </summary>
        public static string PlayerClanElite() =>
            HighestTier(Player.Clan?.RosterElite) ?? HighestTier(Player.Clan?.RosterBasic);

        /// <summary>
        /// Highest-tier troop in the player kingdom's basic tree (kingdom culture root).
        /// Falls back to <see cref="PlayerClanElite"/> if no kingdom or tree is empty.
        /// </summary>
        public static string PlayerKingdomBasic() =>
            HighestTier(Player.Kingdom?.RosterBasic) ?? PlayerClanBasic();

        /// <summary>
        /// Highest-tier troop in the player kingdom's elite tree (kingdom culture root).
        /// Falls back to <see cref="PlayerKingdomBasic"/> if no kingdom or tree is empty.
        /// </summary>
        public static string PlayerKingdomElite() =>
            HighestTier(Player.Kingdom?.RosterElite) ?? PlayerKingdomBasic();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Implementation                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the string ID of the highest-tier (by <see cref="WCharacter.Tier"/>) troop
        /// in the given list, or null if the list is null or empty.
        /// </summary>
        private static string HighestTier(IEnumerable<WCharacter> roster)
        {
            if (roster == null)
                return null;

            WCharacter best = null;

            foreach (var c in roster)
            {
                if (c?.Base == null)
                    continue;

                if (best == null || c.Tier > best.Tier)
                    best = c;
            }

            return best?.StringId;
        }
    }
}
