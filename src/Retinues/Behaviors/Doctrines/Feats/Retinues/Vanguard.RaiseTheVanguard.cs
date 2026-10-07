using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Hire 100 retinues.
    /// </summary>
    public sealed class Feat_Vanguard_RaiseTheVanguard : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.VA_RaiseTheVanguard.Id;

        protected override void OnPlayerRecruitedTroops(WCharacter troop, int amount)
        {
            if (amount <= 0 || troop == null || !troop.IsRetinue)
                return; // Not a retinue troop.

            Feat.Add(amount);
        }

        protected override void OnPlayerUpgradedTroops(WCharacter source, WCharacter target, int number)
        {
            if (source == null || target == null)
                return;

            if (CountsAsNewRetinues(source.SourceFlags, target.SourceFlags, number))
                Feat.Add(number);
        }

        // V2 retinue hiring normally happens through the party-screen conversion path.
        // Replacing one existing retinue with another does not recruit an additional unit.
        internal static bool CountsAsNewRetinues(TroopSourceFlags source, TroopSourceFlags target, int number) =>
            number > 0
            && (source & TroopSourceFlags.Retinue) == 0
            && (target & TroopSourceFlags.Retinue) != 0;
    }
}
