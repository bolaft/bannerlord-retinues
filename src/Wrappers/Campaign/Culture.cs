using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class WCulture(CultureObject culture) : IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly CultureObject _culture = culture;

        public object Base => _culture;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _culture.Name.ToString();

        public string StringId => _culture.StringId.ToString();

        // =========================================================================
        // Items
        // =========================================================================

        public IEnumerable<WItem> Items
        {
            get
            {
                // Iterate over all items
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                {
                    // Yield those that have the correct culture id
                    if (item.Culture?.StringId == StringId)
                        yield return new WItem(item);
                }
            }
        }

        // =========================================================================
        // Troops
        // =========================================================================

        public WCharacter RootBasic => new(_culture.BasicTroop);
        public WCharacter RootElite => new(_culture.EliteBasicTroop);

        public List<WCharacter> EliteTroops
        {
            get
            {
                var list = new List<WCharacter>();
                foreach (var troop in GetTroopTree(RootElite))
                    list.Add(troop);
                return list;
            }
        }

        public List<WCharacter> BasicTroops
        {
            get
            {
                var list = new List<WCharacter>();
                foreach (var troop in GetTroopTree(RootBasic))
                    list.Add(troop);
                return list;
            }
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private IEnumerable<WCharacter> GetTroopTree(WCharacter root)
        {
            var rootTroop = new WCharacter((CharacterObject)root.Base);
            yield return rootTroop;
            if (root.UpgradeTargets != null)
            {
                foreach (var child in root.UpgradeTargets)
                {
                    foreach (var descendant in GetTroopTree(new WCharacter(child)))
                        yield return descendant;
                }
            }
        }
    }
}
