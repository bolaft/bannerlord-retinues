using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Configuration;
using Retinues.Framework.Runtime;
using Retinues.Modules;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Helpers
{
    public static class SkillsHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Caps                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static int ClampTier(int tier) => Math.Max(0, Math.Min(7, tier));

        public static int GetSkillCapForTier(int tier)
        {
            tier = ClampTier(tier);
            return tier switch
            {
                0 => Settings.SkillCapT0,
                1 => Settings.SkillCapT1,
                2 => Settings.SkillCapT2,
                3 => Settings.SkillCapT3,
                4 => Settings.SkillCapT4,
                5 => Settings.SkillCapT5,
                6 => Settings.SkillCapT6,
                7 => Settings.SkillCapT7,
                _ => Settings.SkillCapT7,
            };
        }

        public static int GetSkillTotalForTier(int tier)
        {
            tier = ClampTier(tier);
            return tier switch
            {
                0 => Settings.SkillTotalT0,
                1 => Settings.SkillTotalT1,
                2 => Settings.SkillTotalT2,
                3 => Settings.SkillTotalT3,
                4 => Settings.SkillTotalT4,
                5 => Settings.SkillTotalT5,
                6 => Settings.SkillTotalT6,
                7 => Settings.SkillTotalT7,
                _ => Settings.SkillTotalT7,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Options                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly struct SkillListOptions(
            bool includeHeroOnly,
            bool includeDlc,
            bool includeModded
        )
        {
            public readonly bool IncludeHeroOnly = includeHeroOnly;
            public readonly bool IncludeDlc = includeDlc;
            public readonly bool IncludeModded = includeModded;

            public static SkillListOptions ForCharacter(bool isHeroLike) =>
                new(includeHeroOnly: isHeroLike, includeDlc: isHeroLike, includeModded: true);

            public static SkillListOptions ForHero() =>
                new(includeHeroOnly: true, includeDlc: true, includeModded: true);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Known categories                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // These are "hero-only" skills.
        public static readonly HashSet<string> HeroSkillIds =
        [
            "Crafting",
            "Tactics",
            "Scouting",
            "Roguery",
            "Charm",
            "Leadership",
            "Trade",
            "Steward",
            "Medicine",
            "Engineering",
        ];

        // DLC skill ids (kept explicit; the check for the DLC being loaded is dynamic).
        public static readonly HashSet<string> NavalDLCSkillIds =
        [
            "Mariner",
            "Boatswain",
            "Shipmaster",
        ];

        static bool IsHeroOnlyId(string id) =>
            !string.IsNullOrEmpty(id)
            && (HeroSkillIds.Contains(id) || NavalDLCSkillIds.Contains(id));

        static bool IsDlcId(string id) =>
            !string.IsNullOrEmpty(id) && NavalDLCSkillIds.Contains(id);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static HashSet<string> _defaultSkillIds;
        static HashSet<string> DefaultSkillIds
        {
            get
            {
                if (_defaultSkillIds != null)
                    return _defaultSkillIds;

                _defaultSkillIds = new HashSet<string>(
                    typeof(DefaultSkills)
                        .GetProperties(BindingFlags.Public | BindingFlags.Static)
                        .Where(p => p.PropertyType == typeof(SkillObject))
                        .Select(p => p.GetValue(null) as SkillObject)
                        .Where(s => s != null && !string.IsNullOrEmpty(s.StringId))
                        .Select(s => s.StringId)
                );

                return _defaultSkillIds;
            }
        }

        static List<SkillObject> _allSkillsCached;

        static List<SkillObject> GetAllSkillsFromObjectManager()
        {
            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return [];

            // Includes vanilla + DLC (if loaded) + modded.
            return [.. mgr.GetObjectTypeList<SkillObject>().Where(s => s != null)];
        }

        [StaticClearAction]
        public static void ClearCache()
        {
            _defaultSkillIds = null;
            _allSkillsCached = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<SkillObject> GetSkillList(SkillListOptions options)
        {
            _allSkillsCached ??= GetAllSkillsFromObjectManager();

            var all = _allSkillsCached;
            if (all == null || all.Count == 0)
                return [];

            bool navalLoaded = Mods.NavalDLC.IsLoaded;

            var known = all.Where(s =>
                !string.IsNullOrEmpty(s.StringId) && DefaultSkillIds.Contains(s.StringId)
            );
            var modded = all.Where(s =>
                !string.IsNullOrEmpty(s.StringId) && !DefaultSkillIds.Contains(s.StringId)
            );

            IEnumerable<SkillObject> filteredKnown = known;
            IEnumerable<SkillObject> filteredModded = modded;

            // Hero-only gating (apply to both buckets)
            if (!options.IncludeHeroOnly)
            {
                filteredKnown = filteredKnown.Where(s => !IsHeroOnlyId(s.StringId));
                filteredModded = filteredModded.Where(s => !IsHeroOnlyId(s.StringId));
            }

            // DLC gating (apply to both buckets)
            if (!options.IncludeDlc || !navalLoaded)
            {
                filteredKnown = filteredKnown.Where(s => !IsDlcId(s.StringId));
                filteredModded = filteredModded.Where(s => !IsDlcId(s.StringId));
            }

            var list = new List<SkillObject>();
            list.AddRange(filteredKnown);

            if (options.IncludeModded)
                list.AddRange(filteredModded);

            return
            [
                .. list.Where(s => s != null && !string.IsNullOrEmpty(s.StringId))
                    .GroupBy(s => s.StringId)
                    .Select(g => g.First())
                    .OrderBy(s => DefaultSkillIds.Contains(s.StringId) ? 0 : 1)
                    .ThenBy(s => s.StringId),
            ];
        }

        public static List<SkillObject> GetSkillListForCharacter(
            bool isHeroLike,
            bool includeModded = true
        ) =>
            GetSkillList(
                new SkillListOptions(
                    includeHeroOnly: isHeroLike,
                    includeDlc: isHeroLike,
                    includeModded: includeModded
                )
            );

        public static List<SkillObject> GetSkillListForHero() =>
            GetSkillList(SkillListOptions.ForHero());

        public static SkillObject IdToSkill(string id) =>
            string.IsNullOrEmpty(id) ? null : MBObjectManager.Instance?.GetObject<SkillObject>(id);
    }
}
