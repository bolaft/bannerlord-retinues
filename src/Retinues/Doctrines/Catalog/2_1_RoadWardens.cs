using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class RoadWardens : Doctrine
    {
        public override TextObject Name => L.T("road_wardens", "Road Wardens");
        public override TextObject Description =>
            L.T("road_wardens_description", "Unlocks caravan troops.");
        public override int Column => 2;
        public override int Row => 1;
        public override bool IsDisabled => Config.NoDoctrineRequirements;
        public override TextObject DisabledMessage =>
            L.T("road_wardens_disabled_message", "Disabled: special troops unlocked from config.");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RW_OwnThreeCaravans : Feat
        {
            public override TextObject Description =>
                L.T("road_wardens_three_caravans", "Own three caravans at the same time.");

            public override int Target => 3;

            public override void OnDailyTick()
            {
                var heroIds = Player.Clan?.Base.Heroes?.Select(h => h.StringId);
                if (heroIds?.Count() == 0)
                    return;

                int count = 0;

                foreach (var party in WParty.All)
                {
                    if (!party.IsCaravan)
                        continue;

                    if (party.Leader == null)
                        continue;

                    if (!heroIds.Contains(party.Leader.StringId))
                        continue;

                    count++;
                }

                if (count > Progress)
                    SetProgress(count);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RW_CaravanEscortQuests : Feat
        {
            public override TextObject Description =>
                L.T("road_wardens_caravan_escort_quests", "Complete 3 caravan escort quests.");

            public override int Target => 3;

            public override void OnQuestCompleted(Quest quest)
            {
                if (!quest.IsSuccessful)
                    return;

                if (quest.StringId == "issue_107_quest")
                    AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RW_ClearHideout : Feat
        {
            public override TextObject Description =>
                L.T("road_wardens_clear_hideout", "Clear a bandit hideout.");

            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsHideout)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
