using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class PragmaticScavengers : Doctrine
    {
        public override string Name => L.S("pragmatic_scavengers", "Pragmatic Scavengers");
        public override string Description => L.S("pragmatic_scavengers_description", "Can unlock items from allied party casualties.");
        public override int Column => 0;
        public override int Row => 2;

        public sealed class PS_DefenseAllies50 : Feat
        {
            public override string Description => L.S("pragmatic_scavengers_defense_allies_50", "Win a defensive battle in which allies suffer over 50% casualties.");
            public override int Target => 1;

            private static int AllyCountAtStart = 0;

            public override void OnBattleStart(Battle battle)
            {
                AllyCountAtStart = battle.AllyTroopCount;
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (!battle.PlayerIsDefender) return;
                if (AllyCountAtStart == 0) return; // No allies at start
                if (battle.AllyTroopCount > AllyCountAtStart / 2) return;

                AdvanceProgress(1);
            }
        }

        public sealed class PS_ArmyWinAllies50 : Feat
        {
            public override string Description => L.S("pragmatic_scavengers_army_win_allies_50", "While in an army, win a battle in which allies suffer over 50% casualties.");
            public override int Target => 1;

            private static int AllyCountAtStart = 0;

            public override void OnBattleStart(Battle battle)
            {
                AllyCountAtStart = battle.AllyTroopCount;
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (!Player.Party.IsInArmy) return;
                if (battle.IsLost) return;
                if (!battle.PlayerIsDefender) return;
                if (AllyCountAtStart == 0) return; // No allies at start
                if (battle.AllyTroopCount > AllyCountAtStart / 2) return;

                AdvanceProgress(1);
            }
        }

        public sealed class PS_RescueAlliedLord : Feat
        {
            public override string Description => L.S("pragmatic_scavengers_rescue_allied_lord", "Rescue a defeated lord from captivity.");
            public override int Target => 1;

            private static bool AllyLordInCaptivity = false;

            public override void OnBattleStart(Battle battle)
            {
                foreach (var p in battle.EnemyPrisoners)
                {
                    if (p.IsHero)
                    {
                        AllyLordInCaptivity = true;
                        return;

                    }
                    AllyLordInCaptivity = false;
                }
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost) return;
                if (!AllyLordInCaptivity) return;
                AdvanceProgress(1);
            }
        }
    }
}
