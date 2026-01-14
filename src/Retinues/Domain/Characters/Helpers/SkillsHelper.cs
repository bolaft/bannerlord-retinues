using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
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

        /// <summary>
        /// Clamps the given tier to valid range.
        /// </summary>
        static int ClampTier(int tier) =>
            Math.Max(0, Math.Min(Mods.T7TroopUnlocker.IsLoaded ? 7 : 6, tier));

        /// <summary>
        /// Gets the skill cap for the given wrapped character.
        /// </summary>
        public static int GetSkillCap(WCharacter wc)
        {
            var tier = ClampTier(wc.Tier);
            var cap = tier switch
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

            // Add bonus if enabled
            var bonus = wc.IsRetinue ? Settings.RetinueSkillCapBonus : 0;

            return cap + bonus;
        }

        /// <summary>
        /// Gets the skill total for the given wrapped character.
        /// </summary>
        public static int GetSkillTotal(WCharacter wc)
        {
            var tier = ClampTier(wc.Tier);
            var total = tier switch
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

            // Add bonus if enabled
            var bonus = wc.IsRetinue ? Settings.RetinueSkillTotalBonus : 0;

            return total + bonus;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Options                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Options for skill list retrieval.
        /// </summary>
        public readonly struct SkillListOptions(
            bool includeHeroOnly,
            bool includeDlc,
            bool includeModded
        )
        {
            public readonly bool IncludeHeroOnly = includeHeroOnly;
            public readonly bool IncludeDlc = includeDlc;
            public readonly bool IncludeModded = includeModded;

            /// <summary>
            /// Creates options for the given character.
            /// </summary>
            public static SkillListOptions ForCharacter() =>
                new(includeHeroOnly: false, includeDlc: false, includeModded: true);

            /// <summary>
            /// Creates options for hero characters.
            /// </summary>
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

        [StaticClearAction]
        public static void ClearSkillCaches()
        {
            _defaultSkillIds = null;
            _allSkills = null;
        }

        /// <summary>
        /// Default skill IDs known to the game.
        /// </summary>
        static HashSet<string> _defaultSkillIds;
        static HashSet<string> DefaultSkillIds =>
            _defaultSkillIds ??= [
                .. typeof(DefaultSkills)
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(p => p.PropertyType == typeof(SkillObject))
                    .Select(p => p.GetValue(null) as SkillObject)
                    .Where(s => s != null && !string.IsNullOrEmpty(s.StringId))
                    .Select(s => s.StringId),
            ];

        /// <summary>
        /// All skills known to the object manager.
        /// </summary>
        static List<SkillObject> _allSkills;
        static List<SkillObject> AllSkills =>
            _allSkills ??= [
                .. MBObjectManager.Instance?.GetObjectTypeList<SkillObject>().Where(s => s != null)
                    ?? [],
            ];

        /// <summary>
        /// Gets the skill list based on the given options.
        /// </summary>
        private static List<SkillObject> GetSkillList(SkillListOptions options)
        {
            var all = AllSkills;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the skill list for the given character.
        /// </summary>
        public static List<SkillObject> GetSkillList(WCharacter character) =>
            character.IsHero
                ? GetSkillList(SkillListOptions.ForHero())
                : GetSkillList(SkillListOptions.ForCharacter());

        /// <summary>
        /// Gets the skill list for the given hero.
        /// </summary>
        public static List<SkillObject> GetSkillList(WHero _) =>
            GetSkillList(SkillListOptions.ForHero());
    }
}
