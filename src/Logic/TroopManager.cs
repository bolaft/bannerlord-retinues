using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops
{
    public static class TroopManager
    {
        public static IEnumerable<CharacterWrapper> GetTroops()
        {
            var objs = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
            foreach (var c in objs)
            {
                var id = c?.StringId;
                if (string.IsNullOrEmpty(id)) continue;
                if (id.StartsWith(CharacterWrapper.IdPrefix))
                    yield return new CharacterWrapper(c);
            }
        }

        public static IEnumerable<CharacterWrapper> GetEliteTroops()
        {
            foreach (var troop in GetTroops())
            {
                if (troop.StringId.StartsWith(CharacterWrapper.EliteIdPrefix))
                    yield return troop;
            }
        }

        public static IEnumerable<CharacterWrapper> GetBasicTroops()
        {
            foreach (var troop in GetTroops())
            {
                if (troop.StringId.StartsWith(CharacterWrapper.BasicIdPrefix))
                    yield return troop;
            }
        }

        public static bool CustomTroopsExist()
        {
            var exists = GetTroops().Any();

            if (exists)
            {
                Log.Info($"[TroopManager] Custom troops detected.");
            }
            else
            {
                Log.Info($"[TroopManager] No custom troops found.");
            }

            return exists;
        }
    }
}
