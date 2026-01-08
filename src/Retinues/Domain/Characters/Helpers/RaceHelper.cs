using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Helpers
{
    public static class RaceHelper
    {
        static string[] _raceNamesCache;

        public static int GetRaceCount()
        {
            try
            {
                return FaceGen.GetRaceCount();
            }
            catch
            {
                return 0;
            }
        }

        public static bool HasAlternateSpecies() => GetRaceCount() > 1;

        public static string[] GetRaceNames() => _raceNamesCache ??= FaceGen.GetRaceNames() ?? [];

        public static string FormatRaceName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Replace('_', ' ').Trim();

            if (raw.Length == 1)
                return raw.ToUpperInvariant();

            return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
        }

        public static string GetRaceTitle(int race)
        {
            var names = GetRaceNames();

            string title = null;
            if (names != null && race >= 0 && race < names.Length)
                title = FormatRaceName(names[race]);

            return title ?? $"Race {race}";
        }

        public static string GetRaceText(WCharacter wc)
        {
            if (wc == null)
                return null;

            return GetRaceTitle(wc.Race);
        }

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

        public static bool IsRaceCompatible(WCulture culture, bool isFemale, int race)
        {
            var valid = GetValidRacesFor(culture, isFemale);
            return valid.Count == 0 || valid.Contains(race);
        }

        public static bool HasTemplateForRace(WCulture culture, bool isFemale, int race)
        {
            if (culture == null)
                return false;

            return culture.Troops.Any(t => t != null && t.IsFemale == isFemale && t.Race == race);
        }

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
