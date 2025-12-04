using System.Linq;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class SteadfastSoldiers : Doctrine
    {
        public override TextObject Name => L.T("steadfast_soldiers", "Steadfast Soldiers");
        public override TextObject Description =>
            L.T("steadfast_soldiers_description", "+10 skill points.");
        public override int Column => 3;
        public override int Row => 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SS_TroopsMaxedSkills : Feat
        {
            public override TextObject Description =>
                L.T(
                    "steadfast_soldiers_troops_maxed_skills",
                    "Max out the skills of 15 custom troops."
                );
            public override int Target => 15;

            public override void OnDailyTick()
            {
                int maxedCount = 0;

                foreach (var troop in Player.Troops)
                    if (SkillManager.SkillPointsLeft(troop) == 0)
                        maxedCount++;

                SetProgress(maxedCount);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class SS_SiegeDefenseOnlyCustom : Feat
        {
            public override TextObject Description =>
                L.T(
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

        public sealed class SS_RaiseSecurityTo60 : Feat
        {
            public override TextObject Description =>
                L.T(
                    "steadfast_soldiers_raise_security_60",
                    "Raise the security value of a fief to 60."
                );

            public override int Target => 60;

            public override void OnDailyTick()
            {
                if (Campaign.Current == null || Clan.PlayerClan == null)
                    return;

                // Towns + castles owned by the player clan.
                var fiefs = Clan.PlayerClan.Fiefs.Where(s => s.IsTown || s.IsCastle).ToList();

                if (fiefs.Count == 0)
                    return;

                int maxSecurity = (int)fiefs.Select(s => s.Security).DefaultIfEmpty(0f).Max();

                SetProgress(maxSecurity);
            }
        }
    }
}
