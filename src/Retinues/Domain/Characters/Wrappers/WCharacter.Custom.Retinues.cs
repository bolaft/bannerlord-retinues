using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Services.Matching;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Conversion/retinue-related helpers and cached links for retinue troops.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Conversion Sources                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string ConversionCacheGroupKey = "retinues.wcharacter.conversions";

        /// <summary>
        /// Cached conversion sources for retinues (computed on demand).
        /// </summary>
        public readonly Cache<WCharacter, List<WCharacter>> ConversionCache = new(
            BuildConversionLinks,
            key: ConversionCacheGroupKey
        );

        /// <summary>
        /// List of conversion source troops for this retinue.
        /// </summary>
        public List<WCharacter> ConversionSources => ConversionCache.Get(this);

        /// <summary>
        /// Forces recomputation of conversion sources/targets and reapplies upgrade targets.
        /// </summary>
        public void RefreshConversionLinks()
        {
            if (!IsRetinue)
                return;

            // Local clear (not group clear) then rebuild.
            ConversionCache.ClearLocal();
            _ = ConversionSources;
        }

        /// <summary>
        /// Refreshes conversion links for all retinues owned by the provided faction.
        /// </summary>
        public static void RefreshRetinueConversions(IBaseFaction faction)
        {
            var roster = faction?.RosterRetinues;
            if (roster == null || roster.Count == 0)
                return;

            for (int i = 0; i < roster.Count; i++)
            {
                var r = roster[i];
                if (r?.Base == null)
                    continue;

                if (!r.IsRetinue)
                    continue;

                r.RefreshConversionLinks();
            }
        }

        /// <summary>
        /// Builds conversion sources for the given retinue and updates requirements.
        /// The custom upgrade tree (UpgradeTargets) is user-managed and must not be
        /// overwritten here - doing so corrupts persistence and breaks the editor tree.
        /// </summary>
        private static List<WCharacter> BuildConversionLinks(WCharacter wc)
        {
            if (wc == null || !wc.IsRetinue)
                return [];

            var belowTier = wc.Tier - 1;

            var sources = belowTier >= 1 ? BuildConversionMatches(wc, belowTier) : [];

            // Update item requirements based on conversion sources.
            // UpgradeTargets are traversed recursively by UpdateItemRequirementsFromSources,
            // so the custom tree (T1→T2→T3) is correctly maintained.
            wc.UpdateItemRequirementsFromSources(sources, updateTargets: true);

            return sources;
        }

        /// <summary>
        /// Finds best conversion matches for a retinue at the requested tier.
        /// </summary>
        private static List<WCharacter> BuildConversionMatches(WCharacter wc, int requestedTier)
        {
            if (wc == null)
                return [];

            var results = new List<WCharacter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1) Best match in each of the troop's factions (excluding culture to avoid double counting).
            var factions = wc.Factions;
            if (factions != null && factions.Count > 0)
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    var faction = factions[i];
                    if (faction == null)
                        continue;

                    if (faction is WCulture)
                        continue;

                    var match = CharacterMatcher.FindBest(
                        troop: wc,
                        troops: faction.Troops.Where(t => t.IsRegular),
                        strictTierMatch: true,
                        strictCategoryMatch: false,
                        fallback: null,
                        requestedTier: requestedTier
                    );

                    if (match == null)
                        continue;

                    var id = match.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (seen.Add(id))
                        results.Add(match);
                }
            }

            // 2) Best match in culture.
            var culture = wc.Culture;
            if (culture != null)
            {
                var match = CharacterMatcher.FindBest(
                    troop: wc,
                    troops: culture.Troops.Where(t => t.IsRegular),
                    strictTierMatch: true,
                    strictCategoryMatch: false,
                    fallback: null,
                    requestedTier: requestedTier
                );

                if (match != null)
                {
                    var id = match.StringId;
                    if (!string.IsNullOrEmpty(id) && seen.Add(id))
                        results.Add(match);
                }
            }

            return results;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Retinue Upgrade Targets                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns player-owned retinues that consider the given troop as a conversion source.
        /// </summary>
        public static List<WCharacter> GetPlayerRetinuesForSource(WCharacter source)
        {
            if (source?.Base == null)
                return [];

            var results = new List<WCharacter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var sourceId = source.StringId;
            if (string.IsNullOrEmpty(sourceId))
                return [];

            void AddFromFaction(IBaseFaction faction)
            {
                var roster = faction?.RosterRetinues;
                if (roster == null || roster.Count == 0)
                    return;

                for (int i = 0; i < roster.Count; i++)
                {
                    var retinue = roster[i];
                    if (retinue?.Base == null)
                        continue;

                    // Safety: only consider real retinues
                    if (!retinue.IsRetinue)
                        continue;

                    var conv = retinue.ConversionSources;
                    if (conv == null || conv.Count == 0)
                        continue;

                    var has = false;
                    for (int j = 0; j < conv.Count; j++)
                    {
                        if (conv[j]?.StringId == sourceId)
                        {
                            has = true;
                            break;
                        }
                    }

                    if (!has)
                        continue;

                    var rid = retinue.StringId;
                    if (string.IsNullOrEmpty(rid))
                        continue;

                    if (seen.Add(rid))
                        results.Add(retinue);
                }
            }

            // Clan retinues: always relevant for player
            AddFromFaction(Player.Clan);

            // Kingdom retinues: only if player is the ruler
            if (Player.IsRuler)
                AddFromFaction(Player.Kingdom);

            return results;
        }
    }
}
