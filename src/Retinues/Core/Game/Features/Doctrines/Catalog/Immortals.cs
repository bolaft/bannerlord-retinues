using System.Collections.Generic;
using Retinues.Core.Game.Events;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class Immortals : Doctrine
    {
        public override string Name => L.S("immortals", "Immortals");
        public override string Description =>
            L.S("immortals_description", "+20% retinue survival chance.");
        public override int Column => 3;
        public override int Row => 3;

        public sealed class IM_100RetinueSurviveStruckDown : Feat
        {
            public override string Description =>
                L.S(
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

                if (diff >= 0)
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

        public sealed class IM_Win100NoDeaths : Feat
        {
            public override string Description =>
                L.S(
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
                    if (kill.Victim.IsPlayerTroop)
                        return; // Troop death detected
                }

                AdvanceProgress(1);
            }
        }

        public sealed class IM_SingleRetinueTwoTier5Kills : Feat
        {
            public override string Description =>
                L.S(
                    "immortals_single_retinue_two_tier_5_kills",
                    "Have a single retinue troop kill two tier 5+ units in one battle."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                var killerCandidates = new List<Agent>();

                foreach (var kill in battle.Kills)
                {
                    if (kill.Victim.Character.Tier < 5)
                        continue; // Not tier 5+
                    if (!kill.Killer.Character.IsRetinue)
                        continue; // Not a retinue

                    if (killerCandidates.Contains(kill.Killer.Agent))
                    {
                        // Second tier 5 kill by same retinue
                        AdvanceProgress(1);
                        return;
                    }
                    else
                    {
                        // First tier 5 kill by this retinue
                        killerCandidates.Add(kill.Killer.Agent);
                    }
                }
            }
        }
    }
}
