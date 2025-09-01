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
        private static List<CharacterWrapper> _eliteCustomTroops = new List<CharacterWrapper>();

        private static List<CharacterWrapper> _basicCustomTroops = new List<CharacterWrapper>();

        public static IEnumerable<CharacterWrapper> EliteCustomTroops => _eliteCustomTroops;

        public static IEnumerable<CharacterWrapper> BasicCustomTroops => _basicCustomTroops;

        public static void AddBasicTroop(CharacterWrapper troop)
        {
            if (troop != null && !_basicCustomTroops.Contains(troop))
                _basicCustomTroops.Add(troop);
        }

        public static void AddEliteTroop(CharacterWrapper troop)
        {
            if (troop != null && !_eliteCustomTroops.Contains(troop))
                _eliteCustomTroops.Add(troop);
        }

        public static bool IsNoble(CharacterWrapper troop)
        {
            return EliteCustomTroops.Contains(troop);
        }

        public static bool CustomTroopsExist()
        {
            return BasicCustomTroops.Any() || EliteCustomTroops.Any();
        }

        public static void RemoveTroop(CharacterWrapper troop)
        {
            _basicCustomTroops.Remove(troop);
            _eliteCustomTroops.Remove(troop);

            troop.Parent?.RemoveUpgradeTarget(troop);

            troop.HiddenInEncyclopedia = true;
            troop.IsNotTransferableInPartyScreen = false;
            troop.IsNotTransferableInHideouts = false;
        }
    }
}
