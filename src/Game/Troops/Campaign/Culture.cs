using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Game.Troops.Objects;

namespace CustomClanTroops.Game.Troops.Campaign
{
    public class TroopCulture(CultureObject culture) : CultureWrapper(culture)
    {

        // =========================================================================
        // Items
        // =========================================================================

        public IEnumerable<TroopItem> Items
        {
            get
            {
                // Iterate over all items
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                {
                    // Yield those that have the correct culture id
                    if (item.Culture?.StringId == StringId)
                        yield return new TroopItem(item);
                }
            }
        }

        // =========================================================================
        // Overrides
        // =========================================================================

        public new TroopCharacter RootElite => new(RootElite.Base);

        public new TroopCharacter RootBasic => new(RootBasic.Base);

        // =========================================================================
        // Troops
        // =========================================================================

        public List<TroopCharacter> EliteTroops
        {
            get
            {
                var list = new List<TroopCharacter>();
                foreach (var troop in GetTroopTree(RootElite))
                    list.Add(troop);
                return list;
            }
        }

        public List<TroopCharacter> BasicTroops
        {
            get
            {
                var list = new List<TroopCharacter>();
                foreach (var troop in GetTroopTree(RootBasic))
                    list.Add(troop);
                return list;
            }
        }

        private IEnumerable<TroopCharacter> GetTroopTree(TroopCharacter root)
        {
            var rootTroop = new TroopCharacter(root.Base);
            yield return rootTroop;
            if (root.UpgradeTargets != null)
            {
                foreach (var child in root.UpgradeTargets)
                {
                    foreach (var descendant in GetTroopTree(new TroopCharacter(child)))
                        yield return descendant;
                }
            }
        }
    }
}
