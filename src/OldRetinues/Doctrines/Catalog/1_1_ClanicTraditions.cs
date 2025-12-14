using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
{
    // Note: "Clanic" is preserved in class name for backwards compatibility
    public sealed class ClanicTraditions : Doctrine
    {
        public override TextObject Name => L.T("clan_traditions", "Clan Traditions");
        public override TextObject Description =>
            L.T("clan_traditions_description", "Troops can equip smithed weapons.");
        public override int Column => 1;
        public override int Row => 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CT_OwnSmithy30Days : Feat
        {
            public override TextObject Description =>
                L.T(
                    "clan_traditions_own_smithy_30_days",
                    "Own a smithy in a town of your clan's culture for 30 days."
                );
            public override int Target => 30;

            public override void OnDailyTick()
            {
                foreach (var ws in Hero.MainHero?.OwnedWorkshops)
                {
                    if (ws == null || ws.Settlement == null)
                        continue;
                    if (ws.WorkshopType?.StringId.Contains("smith") == false)
                        continue;
                    if (new WCulture(ws.Settlement.Culture) != Player.Culture)
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
            public override TextObject Description =>
                L.T(
                    "clan_traditions_companions_50_kills",
                    "Win a battle in which you and your companions get 50 or more kills."
                );
            public override int Target => 50;

            public override void OnBattleEnd(Battle battle)
            {
                int heroKills = 0;

                foreach (var kill in battle.Kills)
                {
                    if (kill.KillerIsPlayer || (kill.KillerIsPlayerTroop && kill.Killer.IsHero))
                        heroKills++;
                }

                if (heroKills > Progress)
                    SetProgress(heroKills);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CT_AcquireNewFief : Feat
        {
            public override TextObject Description =>
                L.T("clan_traditions_acquire_new_fief", "Acquire a new fief for your clan.");
            public override int Target => 1;

            public override void OnSettlementOwnerChanged(SettlementOwnerChange change)
            {
                if (change.NewOwner.Clan != Player.Clan)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
