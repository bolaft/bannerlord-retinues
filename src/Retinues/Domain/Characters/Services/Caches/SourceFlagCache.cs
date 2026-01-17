using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Domain.Characters.Services.Caches
{
    /// <summary>
    /// Caches the source flags for troops based on their presence in faction rosters.
    /// </summary>
    [SafeClass]
    public static class SourceFlagCache
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Troop Source Flag Cache               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly object Sync = new();

        private static bool _built;
        private static readonly Dictionary<string, WCharacter.TroopSourceFlags> ById = [];

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        [StaticClearAction]
        public static void Invalidate()
        {
            lock (Sync)
            {
                ById.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Gets the source flags for the given wrapped character.
        /// </summary>
        public static WCharacter.TroopSourceFlags Get(WCharacter wc)
        {
            if (wc == null)
                return WCharacter.TroopSourceFlags.None;

            EnsureBuilt();

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return WCharacter.TroopSourceFlags.None;

            lock (Sync)
            {
                return ById.TryGetValue(id, out var flags)
                    ? flags
                    : WCharacter.TroopSourceFlags.None;
            }
        }

        /// <summary>
        /// Ensures the cache is built.
        /// </summary>
        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (Sync)
            {
                if (_built)
                    return;

                ById.Clear();

                // Culture rosters are the canonical classification layer.
                foreach (var culture in WCulture.All)
                {
                    MarkMany(culture.RosterBasic, WCharacter.TroopSourceFlags.Basic);
                    MarkMany(culture.RosterElite, WCharacter.TroopSourceFlags.Elite);

                    MarkMany(culture.RosterMercenary, WCharacter.TroopSourceFlags.Mercenary);
                    MarkMany(culture.RosterBandit, WCharacter.TroopSourceFlags.Bandit);
                    MarkMany(culture.RosterMilitia, WCharacter.TroopSourceFlags.Militia);
                    MarkMany(culture.RosterCaravan, WCharacter.TroopSourceFlags.Caravan);
                    MarkMany(culture.RosterVillager, WCharacter.TroopSourceFlags.Villager);
                    MarkMany(culture.RosterCivilian, WCharacter.TroopSourceFlags.Civilian);
                }

                // Same for clan rosters.
                foreach (var clan in WClan.All)
                {
                    MarkMany(clan.RosterBasic, WCharacter.TroopSourceFlags.Basic);
                    MarkMany(clan.RosterElite, WCharacter.TroopSourceFlags.Elite);
                }

                // Same for kingdom rosters.
                foreach (var kingdom in WKingdom.All)
                {
                    MarkMany(kingdom.RosterBasic, WCharacter.TroopSourceFlags.Basic);
                    MarkMany(kingdom.RosterElite, WCharacter.TroopSourceFlags.Elite);
                }

                // Retinues live on map-factions (clans/kingdoms).
                foreach (var clan in WClan.All)
                    MarkMany(clan.RosterRetinues, WCharacter.TroopSourceFlags.Retinue);

                foreach (var kingdom in WKingdom.All)
                    MarkMany(kingdom.RosterRetinues, WCharacter.TroopSourceFlags.Retinue);

                _built = true;
            }
        }

        /// <summary>
        /// Marks many wrapped characters with the given flags.
        /// </summary>
        private static void MarkMany(
            IEnumerable<WCharacter> list,
            WCharacter.TroopSourceFlags flags
        )
        {
            if (list == null)
                return;

            foreach (var wc in list)
                Mark(wc, flags);
        }

        /// <summary>
        /// Marks a wrapped character with the given flags.
        /// </summary>
        private static void Mark(WCharacter wc, WCharacter.TroopSourceFlags flags)
        {
            if (wc == null)
                return;

            var id = wc.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (ById.TryGetValue(id, out var existing))
                ById[id] = existing | flags;
            else
                ById.Add(id, flags);
        }
    }
}
