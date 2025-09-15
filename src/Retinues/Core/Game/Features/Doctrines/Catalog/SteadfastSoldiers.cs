using Retinues.Core.Editor;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class SteadfastSoldiers : Doctrine
    {
        public override string Name => L.S("steadfast_soldiers", "Steadfast Soldiers");
        public override string Description =>
            L.S("steadfast_soldiers_description", "+5% skill points.");
        public override int Column => 2;
        public override int Row => 1;

        public sealed class SS_TwelveTroopsMaxedSkills : Feat
        {
            public override string Description =>
                L.S(
                    "steadfast_soldiers_twelve_troops_maxed_skills",
                    "Max out the skills of 15 custom troops."
                );
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
            public override string Description =>
                L.S(
                    "steadfast_soldiers_siege_defense_mostly_custom",
                    "Win a siege defense using mostly custom troops."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.PlayerIsDefender)
                    return;
                if (!battle.IsSiege)
                    return;
                if (Player.Party.MemberRoster.CustomRatio <= 0.5f)
                    return;

                AdvanceProgress(1);
            }
        }

        public sealed class SS_DefendVillageOnlyCustom : Feat
        {
            public override string Description =>
                L.S(
                    "steadfast_soldiers_defend_village_only_custom",
                    "Defend a village from a raid using only custom troops."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsVillageRaid)
                    return;
                if (battle.AllyTroopCount == 0)
                    return; // Heuristic to see if villagers help
                if (Player.Party.MemberRoster.CustomRatio < 0.99f)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
