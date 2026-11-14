using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Compatibility
{
    /// <summary>
    /// Optional War Sails troops. Resolve existing identities only, without referencing
    /// NavalDLC or relying on a troop's editable appearance culture, name or equipment.
    /// </summary>
    internal static class ConvoyTroopResolver
    {
        private static readonly string[] RolePrefixes =
        {
            "naval_caravan_guard_",
            "naval_veteran_caravan_guard_",
            "naval_caravan_master_",
            "naval_armed_trader_",
        };

        internal static List<CharacterObject> Find(CultureObject culture) =>
            Find(culture?.StringId, id => MBObjectManager.Instance?.GetObject<CharacterObject>(id));

        internal static List<CharacterObject> Find(string cultureId, Func<string, CharacterObject> lookup)
        {
            var troops = new List<CharacterObject>();
            if (string.IsNullOrEmpty(cultureId) || lookup == null)
                return troops;

            foreach (var prefix in RolePrefixes)
            {
                var id = prefix + cultureId;
                var troop = lookup(id);
                if (troop != null && troop.StringId == id && !troop.IsHero
                    && troop.Occupation == Occupation.CaravanGuard)
                    troops.Add(troop);
            }

            return troops;
        }
    }
}
