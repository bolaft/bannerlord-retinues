using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Game;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Features.Doctrines.Catalog
{
    public sealed class ClanicTraditions : Doctrine
    {
        public override string Name => L.S("clanic_traditions", "Clan Traditions");
        public override string Description =>
            L.S("clanic_traditions_description", "Troops can equip smithed weapons.");
        public override int Column => 1;
        public override int Row => 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CT_OwnSmithy30Days : Feat
        {
            public override string Description =>
                L.S(
                    "clanic_traditions_own_smithy_30_days",
                    "Own a smithy in a town of your clan's culture for 30 days."
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CT_Companions50Kills : Feat
        {
            public override string Description =>
                L.S(
                    "clanic_traditions_companions_50_kills",
                    "Win a battle in which you and your companions get 50 or more kills."
                );
            public override int Target => 50;

            public override void OnBattleEnd(Battle battle)
            {
                int heroKills = 0;

                foreach (var kill in battle.Kills)
                {
                    if (
                        kill.Killer.IsPlayer
                        || (kill.Killer.IsPlayerTroop && kill.Killer.Character.IsHero)
                    )
                        heroKills++;
                }

                if (heroKills > Progress)
                    SetProgress(heroKills);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CT_AcquireNewFief : Feat
        {
            public override string Description =>
                L.S("clanic_traditions_acquire_new_fief", "Acquire a new fief for your clan.");
            public override int Target => 1;

            public override void OnSettlementOwnerChanged(SettlementOwnerChange change)
            {
                var newOwner = change.NewOwner.Base;
                if (newOwner.HeroObject?.Clan?.StringId != Player.Clan.StringId)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
