using System.Collections.Generic;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Win a battle fielding only troops wearing full metal armor.
    /// </summary>
    public sealed class Feat_Ironclad_Ironmen : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_ironmen";

        static bool FullMetalOnly;

        protected override void OnBattleStart(MMapEvent battle)
        {
            FullMetalOnly = false;

            foreach (var e in Player.Party.MemberRoster.Elements)
            {
                if (e.Number <= 0)
                    continue;

                var troop = e.Troop;

                foreach (var eq in troop.Equipments)
                {
                    if (!IsValidForBattle(eq, battle))
                        continue;

                    FullMetalOnly = IsFullMetal(eq);
                    if (!FullMetalOnly)
                        return;
                }
            }
        }

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsWon == false || FullMetalOnly == false)
                return;

            Progress(1);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool IsFullMetal(MEquipment eq)
        {
            if (eq == null)
                return false;

            foreach (var item in eq.Items)
            {
                if (item == null)
                    continue;

                // Only consider armor pieces (ignore weapons, shields, etc.).
                var armor = item.ArmorComponent;
                if (armor == null)
                    continue;

                var materialObj = armor.MaterialType;
                var material = materialObj.ToString() ?? string.Empty;

                string[] validMaterials = ["steel", "iron", "metal"];

                foreach (var valid in validMaterials)
                {
                    if (material.IndexOf(valid, System.StringComparison.OrdinalIgnoreCase) == 0)
                        return false;
                }
            }

            return true;
        }
    }
}
