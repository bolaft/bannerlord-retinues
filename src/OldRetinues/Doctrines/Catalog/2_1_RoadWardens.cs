using System.Linq;
using System.Reflection;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
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
                // Count all active caravans belonging to the player clan.
                var count = WParty.All.Count(p =>
                    p.IsCaravan && p.Clan != null && p.Clan.IsPlayerClan
                );

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
