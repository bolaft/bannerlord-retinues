using System.Linq;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
{
    public sealed class Immortals : Doctrine
    {
        public override TextObject Name => L.T("immortals", "Immortals");
        public override TextObject Description =>
            L.T("immortals_description", "+20% retinue survival chance.");
        public override int Column => 4;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IM_100RetinueSurviveStruckDown : Feat
        {
            public override TextObject Description =>
                L.T(
                    "immortals_100_retinue_survive_struck_down",
                    "Have 100 retinue troops survive being struck down in battle."
                );
            public override int Target => 100;

            private static int WoundedAtBattleStart = 0;

            public override void OnBattleStart(Battle battle)
            {
                WoundedAtBattleStart = CountWoundedRetinues();
            }

            public override void OnBattleEnd(Battle battle)
            {
                int diff = CountWoundedRetinues() - WoundedAtBattleStart;

                if (diff > 0)
                    AdvanceProgress(diff);
            }

            private int CountWoundedRetinues()
            {
                int count = 0;

                foreach (var troop in Player.Party.MemberRoster.Elements)
                {
                    if (!troop.Troop.IsRetinue)
                        continue; // Not a retinue

                    count += troop.WoundedNumber;
                }

                return count;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IM_Win100NoDeaths : Feat
        {
            public override TextObject Description =>
                L.T(
                    "immortals_win_100_no_deaths",
                    "Win by yourself against 100+ enemies without a single death on your side."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (battle.EnemyTroopCount < 100)
                    return;
                if (battle.AllyTroopCount > 0)
                    return;

                foreach (var kill in battle.Kills)
                {
                    if (kill.State != AgentState.Killed)
                        continue; // Not a death
                    if (kill.VictimIsPlayerTroop)
                        return; // Troop death detected
                }

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class IM_Retinue200Enemies : Feat
        {
            public override TextObject Description =>
                L.T(
                    "immortals_retinue_200_enemies",
                    "Have your retinue defeat 200 enemies in a single battle."
                );
            public override int Target => 200;

            public override void OnBattleEnd(Battle battle)
            {
                int retinueKills = battle.Kills.Count(kill =>
                    kill.KillerIsPlayerTroop && kill.Killer.IsRetinue
                );

                if (retinueKills > Progress)
                    SetProgress(retinueKills);
            }
        }
    }
}
