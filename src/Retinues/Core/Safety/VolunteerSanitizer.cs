using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Safety
{
    public static class VolunteerSanitizer
    {
        public static int CleanAllSettlementVolunteers()
        {
            int fixedHeroes = 0, fixedSlots = 0;

            foreach (var stl in Campaign.Current.Settlements)
            {
                if (stl?.Notables == null || stl.Culture == null) continue;

                foreach (var notable in stl.Notables)
                {
                    try
                    {
                        if (notable == null) continue;

                        var fixedForHero = CleanNotableVolunteers(notable, stl.Culture);
                        if (fixedForHero > 0)
                        {
                            fixedHeroes++;
                            fixedSlots += fixedForHero;
                            Log.Warn($"[VolunteerSanitizer] Fixed {fixedForHero} volunteer slot(s) for {notable.Name} in {stl.Name}.");
                        }
                    }
                    catch
                    {
                        continue; // notable is broken
                    }
                }
            }

            if (fixedSlots > 0)
                Log.Info($"[VolunteerSanitizer] Done. Notables fixed: {fixedHeroes}, slots fixed: {fixedSlots}.");

            return fixedSlots;
        }

        // Fixes null/missing CharacterObjects in a notable's volunteer array.
        // Works across versions by trying common property/field names.
        private static int CleanNotableVolunteers(Hero notable, CultureObject culture)
        {
            var volunteers = TryGetVolunteerArray(notable);
            if (volunteers == null || volunteers.Length == 0) return 0;

            int fixedCount = 0;
            for (int i = 0; i < volunteers.Length; i++)
            {
                var troop = volunteers[i];

                bool broken = troop == null
                        || string.IsNullOrEmpty(troop.StringId)
                        || MBObjectManager.Instance.GetObject<CharacterObject>(troop.StringId) == null;

                // NEW: treat custom-inactive as invalid
                bool customInactive = false;
                try
                {
                    if (troop != null)
                    {
                        var w = new WCharacter(troop);
                        customInactive = w.IsCustom && !w.IsActive;
                    }
                }
                catch { }

                if (broken || customInactive)
                {
                    var fallback = GetFallbackVolunteer(culture);
                    if (fallback != null)
                    {
                        if (TrySetVolunteerAtIndex(notable, i, fallback))
                            fixedCount++;
                        else
                        {
                            volunteers[i] = fallback;
                            if (TrySetVolunteerArray(notable, volunteers))
                                fixedCount++;
                        }
                    }
                }
            }

            return fixedCount;
        }

        private static CharacterObject GetFallbackVolunteer(CultureObject culture)
        {
            // Prefer the defined militia basic troops (melee/ranged). Otherwise use basic culture troop.
            var melee  = TryGet<CharacterObject>(culture, "MeleeMilitiaTroop");
            var ranged = TryGet<CharacterObject>(culture, "RangedMilitiaTroop");
            var pick = (MBRandom.RandomFloat < 0.5f ? melee : ranged) ?? melee ?? ranged;

            if (pick != null) return pick;

            // Very last resort: the culture’s “basic” tree root (common names in many builds)
            var basic = TryGet<CharacterObject>(culture, "BasicTroop")
                        ?? TryGet<CharacterObject>(culture, "BasicInfantry")
                        ?? CharacterObject.Find(culture.StringId + "_recruit");
            return basic;
        }

        private static T TryGet<T>(object obj, string propName) where T : class
        {
            try { return Reflector.GetPropertyValue(obj, propName) as T; } catch { }
            try { return Reflector.GetFieldValue<T>(obj, "_" + char.ToLower(propName[0]) + propName.Substring(1)); } catch { }
            try { return Reflector.GetFieldValue<T>(obj, "m_" + char.ToLower(propName[0]) + propName.Substring(1)); } catch { }
            return null;
        }

        // --- Version-safe volunteer accessors ---

        private static CharacterObject[] TryGetVolunteerArray(Hero notable)
        {
            // Common API in many versions: public CharacterObject[] VolunteerTypes { get; }
            try
            {
                var arr = Reflector.GetPropertyValue(notable, "VolunteerTypes") as CharacterObject[];
                if (arr != null) return (CharacterObject[])arr.Clone();
            }
            catch { }

            // Backing fields used in some builds
            foreach (var f in new[] { "_volunteerTypes", "m_volunteerTypes", "volunteerTypes" })
            {
                try
                {
                    var arr = Reflector.GetFieldValue<CharacterObject[]>(notable, f);
                    if (arr != null) return (CharacterObject[])arr.Clone();
                }
                catch { }
            }

            // Some builds expose getter methods per index
            // If needed, you can try reading indices 0..5 via method reflection; omitted unless you hit this path.
            return null;
        }

        private static bool TrySetVolunteerArray(Hero notable, CharacterObject[] arr)
        {
            // Property setter (rare)
            try { Reflector.SetPropertyValue(notable, "VolunteerTypes", arr); return true; } catch { }

            // Backing fields
            foreach (var f in new[] { "_volunteerTypes", "m_volunteerTypes", "volunteerTypes" })
            {
                try { Reflector.SetFieldValue(notable, f, arr); return true; } catch { }
            }
            return false;
        }

        private static bool TrySetVolunteerAtIndex(Hero notable, int index, CharacterObject troop)
        {
            // Some versions have SetVolunteerAtIndex(int, CharacterObject)
            try
            {
                Reflector.InvokeMethod(
                    notable,
                    "SetVolunteerAtIndex",
                    [typeof(int), typeof(CharacterObject)],
                    index,
                    troop
                );
                return true;
            }
            catch { }

            // Some have SetVolunteerType / SetVolunteer
            foreach (var name in new[] { "SetVolunteerAtIndex", "SetVolunteerType", "SetVolunteer" })
            {
                try
                {
                    Reflector.InvokeMethod(
                        notable,
                        name,
                        [typeof(int), typeof(CharacterObject)],
                        index,
                        troop
                    );
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
