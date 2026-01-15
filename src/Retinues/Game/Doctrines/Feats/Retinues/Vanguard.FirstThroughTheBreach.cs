using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Have a retinue get the first melee kill in a siege assault.
    /// </summary>
    public sealed class Feat_Vanguard_FirstThroughTheBreach : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.VA_FirstThroughTheBreach.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!start.IsSiegeBattle)
                return; // Not a siege battle.

            if (!start.AttackerSide.IsPlayerSide)
                return; // Not an assault.

            // First melee kill overall must be by a player retinue.
            foreach (var kill in kills)
            {
                if (kill.IsMissile)
                    continue; // Ignore missile kills.

                if (!kill.Killer.IsPlayerTroop)
                    return; // Killer is not a player troop.

                if (!kill.Killer.Character.IsRetinue)
                    return; // Killer is not a retinue troop.

                Feat.Add();
                return;
            }
        }
    }
}
