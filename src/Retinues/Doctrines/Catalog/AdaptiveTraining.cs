using System.Collections.Generic;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class AdaptiveTraining : Doctrine
    {
        public override TextObject Name => L.T("adaptive_training", "Adaptive Training");
        public override TextObject Description =>
            L.T("adaptive_training_description", "XP is refunded when lowering a troop's skill.");
        public override int Column => 2;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AT_150InEachSkill : Feat
        {
            public override TextObject Description =>
                L.T(
                    "adaptive_training_150_in_each_skill",
                    "For each skill, have at least one custom troop with a level of 150 or higher."
                );
            public override int Target => 8;

            public override void OnDailyTick()
            {
                List<SkillObject> satisfiedSkills = [];

                foreach (var troop in Player.Troops)
                {
                    foreach (var skill in troop.Skills)
                    {
                        if (satisfiedSkills.Contains(skill.Key))
                            continue;
                        if (skill.Value >= 150)
                            satisfiedSkills.Add(skill.Key);
                    }
                }

                SetProgress(satisfiedSkills.Count);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AT_WinWithEvenSplit : Feat
        {
            public override TextObject Description =>
                L.T(
                    "adaptive_training_win_with_even_split",
                    "Win a battle against over 100 enemies using a party evenly split among infantry, cavalry and ranged clan troops"
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.EnemyTroopCount < 100)
                    return;
                if (battle.IsLost)
                    return;

                WRoster playerRoster = Player.Party.MemberRoster;

                if (playerRoster.InfantryRatio < 0.25)
                    return;
                if (playerRoster.ArchersRatio < 0.25)
                    return;
                if (playerRoster.CavalryRatio < 0.25)
                    return;

                AdvanceProgress();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AT_5Weapons : Feat
        {
            public override TextObject Description =>
                L.T(
                    "adaptive_training_5_weapons",
                    "In a single battle, get a kill using five different weapons classes."
                );
            public override int Target => 5;

            public override void OnBattleEnd(Battle battle)
            {
                var classes = new HashSet<int>();
                foreach (var k in battle.Kills)
                    if (k.KillerIsPlayer)
                        classes.Add(k.BlowWeaponClass);
                SetProgress(System.Math.Max(Progress, classes.Count));
            }
        }
    }
}
