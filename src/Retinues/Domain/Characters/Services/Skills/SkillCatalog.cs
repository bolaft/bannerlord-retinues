using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Wrappers;
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
            bool includeModded
        )
        {
            public readonly bool IncludeHeroOnly = includeHeroOnly;
            public readonly bool IncludeDlc = includeDlc;
            public readonly bool IncludeModded = includeModded;

            public static SkillListOptions ForCharacter() =>
                new(includeHeroOnly: false, includeDlc: false, includeModded: true);

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
                filteredKnown = filteredKnown.Where(s => !IsHeroOnlyId(s.StringId));
                filteredModded = filteredModded.Where(s => !IsHeroOnlyId(s.StringId));
            }

            // DLC gating
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

        public static List<SkillObject> GetSkills(WCharacter character) =>
            character != null && character.IsHero
                ? GetSkillList(SkillListOptions.ForHero())
                : GetSkillList(SkillListOptions.ForCharacter());

        public static List<SkillObject> GetSkills(WHero _) =>
            GetSkillList(SkillListOptions.ForHero());

        public static bool IsValidFor(WCharacter character, SkillObject skill)
        {
            if (character == null || skill == null || string.IsNullOrEmpty(skill.StringId))
                return false;

            // Keep this cheap: use the list we already compute and keep deterministic behavior.
            var list = GetSkills(character);
            if (list == null || list.Count == 0)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == skill)
                    return true;
            }

            return false;
        }
    }
}
