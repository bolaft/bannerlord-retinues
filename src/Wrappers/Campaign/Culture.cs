using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class WCulture(CultureObject culture) : StringIdentifier, IWrapper
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

        public override string StringId => _culture.StringId.ToString();

        // =========================================================================
        // Troops
        // =========================================================================

        public WCharacter RootBasic => new(_culture.BasicTroop, null);
        public WCharacter RootElite => new(_culture.EliteBasicTroop, null);
    }
}
