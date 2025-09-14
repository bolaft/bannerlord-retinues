using Retinues.Core.Game.Events;
using Retinues.Core.Editor;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class SteadfastSoldiers : Doctrine
    {
        public override string Name => "Steadfast Soldiers";
        public override string Description => "+5% skill points.";
        public override int Column => 2;
        public override int Row => 1;

        public sealed class SS_TwelveTroopsMaxedSkills : Feat
        {
            public override string Description => "Max out the skills of 15 custom troops.";
            public override int Target => 15;

            public override void OnDailyTick()
            {
                int maxedCount = 0;

                foreach (var troop in Player.Troops)
                    if (TroopRules.SkillPointsLeft(troop) == 0)
                        maxedCount++;

                SetProgress(maxedCount);
            }
        }

        public sealed class SS_SiegeDefenseMostlyCustom : Feat
        {
            public override string Description => "Win a siege defense using mostly custom troops.";
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (!battle.PlayerIsDefender) return;
                if (!battle.IsSiege) return;
                if (Player.Party.MemberRoster.CustomRatio <= 0.5f) return;

                AdvanceProgress(1);
            }
        }

        public sealed class SS_DefendVillageOnlyCustom : Feat
        {
            public override string Description => "Defend a village from a raid using only custom troops.";
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (!battle.IsVillageRaid) return;
                if (battle.AllyTroopCount == 0) return; // Heuristic to see if villagers help
                if (Player.Party.MemberRoster.CustomRatio < 0.99f) return;

                AdvanceProgress(1);
            }
        }
    }
}
