using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Modules.Versions;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Services.Skills
{
    public static class SkillCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Options                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Options for skill list retrieval.
        /// </summary>
        public readonly struct SkillListOptions(
            bool includeHeroOnly,
            bool includeDlc,
            bool includeModded,
            bool includeMarinerForTroop = false
        )
        {
            public readonly bool IncludeHeroOnly = includeHeroOnly;
            public readonly bool IncludeDlc = includeDlc;
            public readonly bool IncludeModded = includeModded;

            /// <summary>
            /// When true, the "Mariner" skill is included even for non-hero troops
            /// (requires DLC loaded + BL 1.4+ + troop's IsMariner flag).
            /// </summary>
            public readonly bool IncludeMarinerForTroop = includeMarinerForTroop;

            public static SkillListOptions ForCharacter() =>
                new(includeHeroOnly: false, includeDlc: false, includeModded: true);

            public static SkillListOptions ForHero() =>
                new(includeHeroOnly: true, includeDlc: true, includeModded: true);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Known Categories                    //
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

        // The Mariner skill applies to troops (not just heroes) starting with BL 1.4.
        // "Boatswain" and "Shipmaster" remain hero-only.
        public const string MarinerSkillId = "Mariner";

        static bool IsMarinerTroopSkillEligible =>
            Mods.NavalDLC.IsLoaded && GameVersion.IsAtLeast14();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [StaticClearAction]
        public static void ClearSkillCaches()
        {
            _defaultSkillIds = null;
            _allSkills = null;
        }

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

        static List<SkillObject> _allSkills;
        static List<SkillObject> AllSkills =>
            _allSkills ??= [
                .. MBObjectManager.Instance?.GetObjectTypeList<SkillObject>().Where(s => s != null)
                    ?? [],
            ];

        /// <summary>
        /// Gets the skill list based on the given options.
        /// </summary>
        static List<SkillObject> GetSkillList(SkillListOptions options)
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

            // Hero-only gating
            if (!options.IncludeHeroOnly)
            {
                filteredKnown = filteredKnown.Where(s =>
                    !IsHeroOnlyId(s.StringId)
                    || (
                        options.IncludeMarinerForTroop
                        && s.StringId == MarinerSkillId
                        && IsMarinerTroopSkillEligible
                    )
                );
                filteredModded = filteredModded.Where(s =>
                    !IsHeroOnlyId(s.StringId)
                    || (
                        options.IncludeMarinerForTroop
                        && s.StringId == MarinerSkillId
                        && IsMarinerTroopSkillEligible
                    )
                );
            }

            // DLC gating
            if (!options.IncludeDlc || !navalLoaded)
            {
                filteredKnown = filteredKnown.Where(s =>
                    !IsDlcId(s.StringId)
                    || (
                        options.IncludeMarinerForTroop
                        && s.StringId == MarinerSkillId
                        && IsMarinerTroopSkillEligible
                    )
                );
                filteredModded = filteredModded.Where(s =>
                    !IsDlcId(s.StringId)
                    || (
                        options.IncludeMarinerForTroop
                        && s.StringId == MarinerSkillId
                        && IsMarinerTroopSkillEligible
                    )
                );
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
        /// Gets the valid skills for the given character.
        /// </summary>
        public static List<SkillObject> GetSkills(WCharacter character) =>
            character != null && character.IsHero
                ? GetSkillList(SkillListOptions.ForHero())
                : GetSkillList(
                    new SkillListOptions(
                        includeHeroOnly: false,
                        includeDlc: false,
                        includeModded: true,
                        includeMarinerForTroop: character?.IsMariner == true
                    )
                );

        /// <summary>
        /// Gets the valid skills for the given hero.
        /// </summary>
        public static List<SkillObject> GetSkills(WHero _) =>
            GetSkillList(SkillListOptions.ForHero());
    }
}
