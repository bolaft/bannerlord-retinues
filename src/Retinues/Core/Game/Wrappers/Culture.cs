using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Wrappers
{
    public class WCulture(CultureObject culture) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CultureObject _culture = culture;

        public CultureObject Base => _culture;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _culture?.Name.ToString();

        public override string StringId => _culture?.StringId?.ToString() ?? Name; // Some cultures have no StringId?

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter RootBasic =>
            _culture?.BasicTroop != null ? new(_culture.BasicTroop) : null;
        public WCharacter RootElite =>
            _culture?.EliteBasicTroop != null ? new(_culture.EliteBasicTroop) : null;

        public WCharacter MilitiaMelee => _culture?.MeleeMilitiaTroop != null ? new(_culture.MeleeMilitiaTroop) : null;
        public WCharacter MilitiaMeleeElite => _culture?.MeleeEliteMilitiaTroop != null ? new(_culture.MeleeEliteMilitiaTroop) : null;
        public WCharacter MilitiaRanged => _culture?.RangedMilitiaTroop != null ? new(_culture.RangedMilitiaTroop) : null;
        public WCharacter MilitiaRangedElite => _culture?.RangedEliteMilitiaTroop != null ? new(_culture.RangedEliteMilitiaTroop) : null;
    }
}
