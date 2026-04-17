using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;

namespace Retinues.Domain.Characters.Helpers
{
    /// <summary>
    /// Factory helpers for building <see cref="Func{String}"/> delegates used as
    /// <c>PreviewCharacterId</c> on doctrine definitions.
    ///
    /// Fallback order for all methods:
    ///   player faction (clan or kingdom) → player culture → null (falls back to player hero)
    /// </summary>
    public static class DoctrinePreviewHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Tree-based (roster)                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a delegate that resolves the highest-tier character in a roster.
        /// Tries player clan, then player culture.
        /// </summary>
        public static Func<string> ClanTree(Func<IBaseFaction, List<WCharacter>> roster) =>
            () => HighestTier(roster(Player.Clan)) ?? HighestTier(roster(Player.Culture));

        /// <summary>
        /// Returns a delegate that resolves the highest-tier character in a roster
        /// directly from the player's culture (for rosters that only exist on cultures,
        /// e.g. bandit troops).
        /// </summary>
        public static Func<string> CultureTree(Func<IBaseFaction, List<WCharacter>> roster) =>
            () => HighestTier(roster(Player.Culture));

        /// <summary>
        /// Returns a delegate that resolves the highest-tier character in a roster.
        /// Tries player kingdom, then player clan, then player culture.
        /// </summary>
        public static Func<string> KingdomTree(Func<IBaseFaction, List<WCharacter>> roster) =>
            () =>
                (Player.Kingdom != null ? HighestTier(roster(Player.Kingdom)) : null)
                ?? HighestTier(roster(Player.Clan))
                ?? HighestTier(roster(Player.Culture));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Single-character slot                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a delegate that resolves a single-character faction slot.
        /// Tries player clan, then player culture.
        /// </summary>
        public static Func<string> FromClan(Func<IBaseFaction, WCharacter> get) =>
            () => get(Player.Clan)?.StringId ?? get(Player.Culture)?.StringId;

        /// <summary>
        /// Returns a delegate that resolves a single-character faction slot.
        /// Tries player kingdom, then player clan, then player culture.
        /// </summary>
        public static Func<string> FromKingdom(Func<IBaseFaction, WCharacter> get) =>
            () =>
                (Player.Kingdom != null ? get(Player.Kingdom)?.StringId : null)
                ?? get(Player.Clan)?.StringId
                ?? get(Player.Culture)?.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Implementation                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
