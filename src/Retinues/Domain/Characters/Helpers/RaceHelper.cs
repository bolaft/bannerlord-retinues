using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Helpers
{
    public static class RaceHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Lists                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the cached array of race names.
        /// </summary>
        static string[] _raceNamesCache;

        public static string[] GetRaceNames() => _raceNamesCache ??= FaceGen.GetRaceNames() ?? [];

        /// <summary>
        /// Gets the total number of defined races.
        /// </summary>
        public static int GetRaceCount() => FaceGen.GetRaceCount();

        /// <summary>
        /// Determines if there are multiple defined races.
        /// </summary>
        public static bool HasAlternateSpecies() => GetRaceCount() > 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the display title for the given race index.
        /// </summary>
        public static string GetRaceName(int race)
        {
            var names = GetRaceNames();

            string title = null;
            if (names != null && race >= 0 && race < names.Length)
                title = FormatRaceName(names[race]);

            return title ?? $"Race {race}";
        }

        /// <summary>
        /// Gets the race name for the given wrapped character.
        /// </summary>
        public static string GetRaceName(WCharacter wc)
        {
            if (wc == null)
                return null;

            return GetRaceName(wc.Race);
        }

        /// <summary>
        /// Formats a raw race name into a user-friendly display name.
        /// </summary>
        public static string FormatRaceName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Replace('_', ' ').Trim();

            if (raw.Length == 1)
                return raw.ToUpperInvariant();

            return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Validation                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given race index has a valid model.
        /// </summary>
        public static bool IsRaceModelValid(int race)
        {
            try
            {
                return FaceGen.GetBaseMonsterFromRace(race) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the list of valid race indices for the given culture and gender.
        /// </summary>
        public static List<int> GetValidRacesFor(WCulture culture, bool isFemale)
        {
            if (culture == null)
                return [];

            var set = new HashSet<int>();

            foreach (var troop in culture.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsFemale != isFemale)
                    continue;

                set.Add(troop.Race);
            }

            if (set.Count == 0)
            {
                foreach (var troop in culture.Troops)
                {
                    if (troop == null)
                        continue;

                    set.Add(troop.Race);
                }
            }

            var list = new List<int>(set);
            list.Sort();
            return list;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Templates                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given culture has a template for the given race.
        /// </summary>
        public static bool HasTemplateForRace(WCulture culture, bool isFemale, int race)
        {
            if (culture == null)
                return false;

            return culture.Troops.Any(t => t != null && t.IsFemale == isFemale && t.Race == race);
        }

        /// <summary>
        /// Finds a template troop for the given culture, gender, and race.
        /// </summary>
        public static WCharacter FindTemplate(WCulture culture, bool isFemale, int race)
        {
            if (culture == null)
                return null;

            var root = culture.RootBasic ?? culture.RootElite;
            if (root != null && root.IsFemale == isFemale && root.Race == race)
                return root;

            var villager = isFemale ? culture.VillageWoman : culture.Villager;
            if (villager != null && villager.IsFemale == isFemale && villager.Race == race)
                return villager;

            foreach (var troop in culture.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsFemale == isFemale && troop.Race == race)
                    return troop;
            }

            return null;
        }
    }
}
