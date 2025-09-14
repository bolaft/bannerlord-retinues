using System.Collections.Generic;
using TaleWorlds.Core;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class AdaptiveTraining : Doctrine
    {
        public override string Name => L.S("adaptive_training", "Adaptive Training");
        public override string Description => L.S("adaptive_training_description", "Experience refunds for retraining.");
        public override int Column => 2;
        public override int Row => 3;

        public sealed class AT_200InEachSkill : Feat
        {
            public override string Description => L.S("adaptive_training_200_in_each_skill", "For each skill, have at least one custom troop with a level of 200 or higher.");
            public override int Target => 8;

            public override void OnDailyTick()
            {
                List<SkillObject> satisfiedSkills = [];
                
                foreach (var troop in Player.Troops)
                {
                    foreach (var skill in troop.Skills)
                    {
                        if (satisfiedSkills.Contains(skill.Key)) continue;
                        if (skill.Value >= 200)
                            satisfiedSkills.Add(skill.Key);
                    }
                }

                SetProgress(satisfiedSkills.Count);
            }
        }

        public sealed class AT_WinWithEvenSplit : Feat
        {
            public override string Description => L.S("adaptive_training_win_with_even_split", "Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops");
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.EnemyTroopCount < 100) return;
                if (battle.IsLost) return;

                WRoster playerRoster = Player.Party.MemberRoster;

                if (playerRoster.InfantryRatio < 0.25) return;
                if (playerRoster.ArchersRatio < 0.25) return;
                if (playerRoster.CavalryRatio < 0.25) return;

                AdvanceProgress();
            }
        }

        public sealed class AT_50TournamentKOs : Feat
        {
            public override string Description => L.S("adaptive_training_50_tournament_kos", "Knock-out 50 opponents in tournaments.");
            public override int Target => 50;

            public override void OnTournamentFinished(Tournament tournament)
            {
                int koCount = 0;

                foreach (var ko in tournament.KnockOuts)
                {
                    if (ko.Killer?.Character.StringId == Player.Character.StringId)
                        koCount++;
                }

                AdvanceProgress(koCount);
            }
        }
    }
}
