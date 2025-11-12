using System.Linq;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class RoyalPatronage : Doctrine
    {
        public override TextObject Name => L.T("royal_patronage", "Royal Patronage");
        public override TextObject Description =>
            L.T("royal_patronage_description", "Unlocks caravan and villager troops.");
        public override int Column => 1;
        public override int Row => 2;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RP_Recruit100CustomKingdom : Feat
        {
            public override TextObject Description =>
                L.T(
                    "royal_patronage_recruit_100_custom_kingdom",
                    "Recruit 100 custom kingdom troops."
                );
            public override int Target => 100;

            public override void OnTroopRecruited(WCharacter troop, int amount)
            {
                if (troop.IsCustom && troop.Faction == Player.Kingdom)
                    AdvanceProgress(amount);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RP_CompanionGovernor30Days : Feat
        {
            public override TextObject Description =>
                L.T(
                    "royal_patronage_companion_governor_30_days",
                    "Have a companion of the same culture as your kingdom govern a kingdom fief for 30 days."
                );
            public override int Target => 30;

            public override void OnDailyTick()
            {
                if (Player.Kingdom == null)
                    return;

                foreach (var s in Player.Clan.Settlements)
                {
                    if (s.Governor.Culture == Player.Kingdom?.Culture)
                    {
                        AdvanceProgress(1);
                        return;
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class RP_1000KillsCustomKingdom : Feat
        {
            public override TextObject Description =>
                L.T(
                    "royal_patronage_1000_kills_custom_kingdom",
                    "Get 1000 kills with custom kingdom troops."
                );
            public override int Target => 1000;

            public override void OnBattleEnd(Battle battle)
            {
                if (Player.Kingdom == null)
                    return;

                int kingdomTroopKills = battle.Kills.Count(k =>
                    k.KillerIsPlayerTroop && !k.KillerIsPlayer && k.Killer.Faction == Player.Kingdom
                );

                AdvanceProgress(kingdomTroopKills);
            }
        }
    }
}
