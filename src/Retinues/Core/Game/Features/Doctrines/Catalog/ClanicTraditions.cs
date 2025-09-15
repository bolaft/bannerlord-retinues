using System.Linq;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class ClanicTraditions : Doctrine
    {
        public override string Name => L.S("clanic_traditions", "Clanic Traditions");
        public override string Description =>
            L.S("clanic_traditions_description", "10% rebate on items of the clan's culture.");
        public override int Column => 1;
        public override int Row => 1;

        public sealed class CT_OwnSmithy30Days : Feat
        {
            public override string Description =>
                L.S(
                    "clanic_traditions_own_smithy_30_days",
                    "Own a smithy in a fief of your clan's culture for 30 days."
                );
            public override int Target => 30;

            public override void OnDailyTick()
            {
                foreach (var ws in Hero.MainHero?.OwnedWorkshops)
                {
                    if (ws == null || ws.Settlement == null)
                        continue;
                    if (ws.WorkshopType?.StringId != "smithy")
                        continue;
                    if (ws.Settlement.Culture.StringId != Player.Culture.StringId)
                        continue;

                    AdvanceProgress(1);
                    return;
                }

                SetProgress(0); // Reset progress if not owning a smithy in a fief of own culture
            }
        }

        public sealed class CT_Companions50Kills : Feat
        {
            public override string Description =>
                L.S(
                    "clanic_traditions_companions_50_kills",
                    "Win a battle in which your companions get 50 or more kills."
                );
            public override int Target => 50;

            public override void OnBattleEnd(Battle battle)
            {
                var companionKills = battle.Kills.Count(k =>
                    k.Killer.Character.IsHero && k.Killer.IsPlayerTroop
                );

                if (companionKills > Progress)
                    SetProgress(companionKills);
            }
        }

        public sealed class CT_CompanionWins100BattleAsLeader : Feat
        {
            public override string Description =>
                L.S(
                    "clanic_traditions_companion_wins_100_battle_as_leader",
                    "Acquire a new fief for your clan."
                );
            public override int Target => 1;

            public override void OnSettlementOwnerChanged(SettlementOwnerChange change)
            {
                var newOwner = (CharacterObject)change.NewOwner.Base;
                if (newOwner.HeroObject?.Clan?.StringId != Player.Clan.StringId)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
