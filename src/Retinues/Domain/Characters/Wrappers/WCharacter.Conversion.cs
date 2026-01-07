using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Conversion Sources                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<WCharacter, List<WCharacter>> _conversionSourcesCache = new(
            BuildConversionSources
        );

        public List<WCharacter> ConversionSources => _conversionSourcesCache.Get(this);

        private static List<WCharacter> BuildConversionSources(WCharacter wc)
        {
            Log.Info($"Building conversion sources for retinue '{wc.Name}'.");
            if (wc == null)
                return [];

            if (!wc.IsRetinue)
                return [];

            var belowTier = wc.Tier - 1;
            if (belowTier < 0)
                return [];

            var results = new List<WCharacter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            // 1) Best match in each of the troop's factions (excluding culture to avoid double counting).
            var factions = wc.Factions;
            if (factions != null && factions.Count > 0)
            {
                Log.Info(
                    $"Building conversion sources for retinue '{wc.Name}': checking {factions.Count} factions."
                );
                for (int i = 0; i < factions.Count; i++)
                {
                    var faction = factions[i];
                    Log.Info($"  Checking faction '{faction?.Name}'.");
                    if (faction == null)
                        continue;

                    if (faction is WCulture)
                        continue;

                    var match = MatcherHelper.PickBestFromFaction(
                        troop: wc,
                        faction: faction,
                        strictTierMatch: true,
                        fallback: null,
                        regularOnly: true,
                        requestedTier: belowTier
                    );

                    Log.Info($"    Best match: '{match?.Name}'.");

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
                var match = MatcherHelper.PickBestFromFaction(
                    troop: wc,
                    faction: culture,
                    strictTierMatch: true,
                    fallback: null,
                    requestedTier: belowTier
                );

                Log.Info($"    Best culture match: '{match?.Name}'.");

                if (match != null)
                {
                    var id = match.StringId;
                    if (!string.IsNullOrEmpty(id) && seen.Add(id))
                        results.Add(match);
                }
            }
            Log.Info($"  Total conversion sources found: {results.Count}.");

            return results;
        }
    }
}
