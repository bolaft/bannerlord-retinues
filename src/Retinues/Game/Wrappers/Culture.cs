using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
#endif

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CultureObject, exposing troop roots and militia for custom logic.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
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

        public string Name => _culture?.Name?.ToString();

        public override string StringId => _culture?.StringId ?? Name; // Some cultures have no StringId?

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public BannerImageIdentifier Image =>
            _culture.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
        public ImageIdentifier ImageIdentifier =>
            _culture.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public BannerCode BannerCode => BannerCode.CreateFrom(Base.BannerKey);
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the basic root troop for this culture.
        /// </summary>
        public WCharacter RootBasic =>
            _culture?.BasicTroop != null ? new(_culture.BasicTroop) : null;

        /// <summary>
        /// Gets the elite root troop for this culture.
        /// </summary>
        public WCharacter RootElite =>
            _culture?.EliteBasicTroop != null ? new(_culture.EliteBasicTroop) : RootBasic;

        /// <summary>
        /// Gets the melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMelee =>
            _culture?.MeleeMilitiaTroop != null ? new(_culture.MeleeMilitiaTroop) : RootBasic;

        /// <summary>
        /// Gets the elite melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMeleeElite =>
            _culture?.MeleeEliteMilitiaTroop != null ? new(_culture.MeleeEliteMilitiaTroop) : MilitiaMelee;

        /// <summary>
        /// Gets the ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRanged =>
            _culture?.RangedMilitiaTroop != null ? new(_culture.RangedMilitiaTroop) : RootBasic;

        /// <summary>
        /// Gets the elite ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRangedElite =>
            _culture?.RangedEliteMilitiaTroop != null
                ? new(_culture.RangedEliteMilitiaTroop)
                : MilitiaRanged;

        /// <summary>
        /// Gets the villager troop for this culture.
        /// </summary>
        public WCharacter Villager => _culture?.Villager != null ? new(_culture.Villager) : RootBasic;

        /// <summary>
        ///  Gets the caravan master troop for this culture.
        /// </summary>
        public WCharacter CaravanMaster =>
            _culture?.CaravanMaster != null ? new(_culture.CaravanMaster) : RootBasic;

        /// <summary>
        ///  Gets the caravan guard troop for this culture.
        /// </summary>
        public WCharacter CaravanGuard =>
            _culture?.CaravanGuard != null ? new(_culture.CaravanGuard) : RootBasic;

        /// <summary>
        /// Gets the prison guard troop for this culture.
        /// </summary>
        public WCharacter PrisonGuard =>
            _culture?.PrisonGuard != null ? new(_culture.PrisonGuard) : RootBasic;
    }
}
