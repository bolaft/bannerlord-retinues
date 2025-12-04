using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class StalwartMilitia : Doctrine
    {
        public override TextObject Name => L.T("stalwart_militia", "Stalwart Militia");
        public override TextObject Description =>
            L.T("stalwart_militia_description", "Unlocks militia troops.");
        public override int Column => 2;
        public override int Row => 0;
        public override bool IsDisabled => Config.NoDoctrineRequirements;
        public override TextObject DisabledMessage =>
            L.T(
                "stalwart_militia_disabled_message",
                "Disabled: special troops unlocked from config."
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SM_DefendCityFromSiege : Feat
        {
            public override TextObject Description =>
                L.T(
                    "stalwart_militia_defend_city_from_siege",
                    "Defend a city against a besieging enemy army."
                );

            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsSiege)
                    return;
                if (!battle.PlayerIsDefender)
                    return;
                if (!battle.EnemyIsInArmy)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SM_PlayerKillsInSiegeDefense : Feat
        {
            public override TextObject Description =>
                L.T(
                    "stalwart_militia_player_kill_50_siegers",
                    "Personally slay 50 assailants during a siege defense."
                );

            public override int Target => 50;

            public override void OnBattleEnd(Battle battle)
            {
                if (!battle.IsSiege)
                    return;
                if (!battle.PlayerIsDefender)
                    return;
                if (battle.IsLost)
                    return;

                // Count player hero kills during qualifying siege defenses.
                int playerKills = battle.Kills.Count(k => k.KillerIsPlayer);

                if (playerKills > Progress)
                    SetProgress(playerKills);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SM_RaiseMilitiaTo400 : Feat
        {
            public override TextObject Description =>
                L.T(
                    "stalwart_militia_raise_militia_400",
                    "Raise the militia value of a fief to 400."
                );

            public override int Target => 400;

            public override void OnDailyTick()
            {
                if (Campaign.Current == null || Clan.PlayerClan == null)
                    return;

                // Towns + castles owned by the player clan.
                var fiefs = Clan.PlayerClan.Settlements.Where(s => s.IsTown || s.IsCastle).ToList();

                if (fiefs.Count == 0)
                    return;

                int maxMilitia = (int)fiefs.Select(s => s.Militia).DefaultIfEmpty(0f).Max();

                SetProgress(maxMilitia);
            }
        }
    }
}
