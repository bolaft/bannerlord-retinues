using Retinues.Core.Editor;
using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Game;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Doctrines.Catalog
{
    public sealed class SteadfastSoldiers : Doctrine
    {
        public override string Name => L.S("steadfast_soldiers", "Steadfast Soldiers");
        public override string Description =>
            L.S("steadfast_soldiers_description", "+10 skill points.");
        public override int Column => 2;
        public override int Row => 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SS_TroopsMaxedSkills : Feat
        {
            public override string Description =>
                L.S(
                    "steadfast_soldiers_troops_maxed_skills",
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SS_SiegeDefenseOnlyCustom : Feat
        {
            public override string Description =>
                L.S(
                    "steadfast_soldiers_siege_defense_only_custom",
                    "Win a siege defense using only custom troops."
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
                if (Player.Party.MemberRoster.CustomRatio < 0.99f)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
                if (Player.Party.MemberRoster.CustomRatio < 0.99f)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
