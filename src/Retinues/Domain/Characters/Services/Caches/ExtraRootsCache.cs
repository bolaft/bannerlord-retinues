using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Services.Caches
{
    /// <summary>
    /// Discovers troop trees that exist outside every known culture/faction roster.
    ///
    /// Overhaul mods (e.g. Realm of Thrones' Household Troops) ship secondary soldier trees for a
    /// culture that are not referenced by any of the culture's canonical fields (basic, elite,
    /// militia, caravan, villager, mercenary, bandit, civilian). Those troops were invisible to the
    /// editor. A troop qualifies as an extra root when it is a non-hero, non-custom Soldier with a
    /// culture, is shown in the encyclopedia (filters out quest/template troops), carries no source
    /// flags (i.e. no roster claims it), and no other troop upgrades into it.
    /// </summary>
    [SafeClass]
    public static class ExtraRootsCache
    {
        private static readonly object Sync = new();

        private static bool _built;
        private static readonly Dictionary<string, List<WCharacter>> ByCultureId = new(
            StringComparer.Ordinal
        );

        /// <summary>
        /// Invalidates the cache.
        /// </summary>
        [StaticClearAction]
        public static void Invalidate()
        {
            lock (Sync)
            {
                ByCultureId.Clear();
                _built = false;
            }
        }

        /// <summary>
        /// Returns the extra (uncategorized) tree roots for the given culture, or an empty list.
        /// </summary>
        public static List<WCharacter> Get(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return [];

            EnsureBuilt();

            lock (Sync)
            {
                return ByCultureId.TryGetValue(cultureId, out var roots) ? roots : [];
            }
        }

        private static void EnsureBuilt()
        {
            if (_built)
                return;

            lock (Sync)
            {
                if (_built)
                    return;

                ByCultureId.Clear();

                // Pass 1: collect candidates and every id that is an upgrade target of a soldier.
                var candidates = new List<WCharacter>();
                var upgradedInto = new HashSet<string>(StringComparer.Ordinal);

                foreach (var wc in WCharacter.All)
                {
                    if (wc?.Base == null || wc.IsHero || wc.IsCustom)
                        continue;

                    if (wc.Base.Occupation != Occupation.Soldier)
                        continue;

                    candidates.Add(wc);

                    var targets = wc.Base.UpgradeTargets;
                    if (targets == null)
                        continue;

                    for (int i = 0; i < targets.Length; i++)
                    {
                        var id = targets[i]?.StringId;
                        if (!string.IsNullOrEmpty(id))
                            upgradedInto.Add(id);
                    }
                }

                // Pass 2: keep roots that no roster claims and nothing upgrades into.
                foreach (var wc in candidates)
                {
                    if (upgradedInto.Contains(wc.StringId))
                        continue; // not a root

                    if (wc.HiddenInEncyclopedia)
                        continue; // quest/template troops

                    if (SourceFlagCache.Get(wc) != TroopSourceFlags.None)
                        continue; // already part of a known roster

                    var cultureId = wc.Culture?.StringId;
                    if (string.IsNullOrEmpty(cultureId))
                        continue;

                    if (!ByCultureId.TryGetValue(cultureId, out var list))
                        ByCultureId[cultureId] = list = [];

                    list.Add(wc);
                }

                _built = true;
            }
        }
    }
}
