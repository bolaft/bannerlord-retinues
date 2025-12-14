using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
{
    public sealed class Vanguard : Doctrine
    {
        public override TextObject Name => L.T("vanguard", "Vanguard");
        public override TextObject Description => L.T("vanguard_description", "+15% retinue cap.");
        public override int Column => 4;
        public override int Row => 2;
        public override bool IsDisabled =>
            Config.MaxBasicRetinueRatio >= 0.85f && Config.MaxEliteRetinueRatio >= 0.85f;
        public override TextObject DisabledMessage =>
            L.T("vanguard_disabled_message", "Disabled: retinue cap already maxed in config.");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class VG_ClearHideoutRetinueOnly : Feat
        {
            public override TextObject Description =>
                L.T(
                    "vanguard_clear_hideout_retinue_only",
                    "Clear a hideout using only your retinue."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!battle.IsHideout)
                    return;

                foreach (var kill in battle.Kills)
                {
                    if (kill.KillerIsPlayer || (kill.KillerIsPlayerTroop && kill.Killer.IsRetinue))
                        continue; // Player or player retinue kill, valid

                    if (kill.VictimIsPlayer || (kill.VictimIsPlayerTroop && kill.Victim.IsRetinue))
                        continue; // Player or player retinue victim, valid

                    return; // Anything else is invalid
                }

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class VG_Win100RetinueOnly : Feat
        {
            public override TextObject Description =>
                L.T("vanguard_win_100_retinue_only", "Win a 100+ battle using only your retinue.");
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (battle.TotalTroopCount < 100)
                    return;
                if (battle.AllyTroopCount > 0)
                    return; // No allies allowed
                if (Player.Party.MemberRoster.RetinueRatio < 1.0f)
                    return; // No non-retinues allowed

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class VG_FirstMeleeKillInSiege : Feat
        {
            public override TextObject Description =>
                L.T(
                    "vanguard_first_melee_kill_in_siege",
                    "Have a retinue get the first melee kill in a siege assault."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (!battle.IsSiege)
                    return;
                if (battle.PlayerIsDefender)
                    return;

                foreach (var kill in battle.Kills)
                {
                    if (kill.IsMissile)
                        continue; // Ignore ranged kills
                    if (!kill.KillerIsPlayerTroop)
                        return; // First melee kill not by player troop
                    if (!kill.Killer.IsRetinue)
                        return; // First melee kill not by retinue

                    AdvanceProgress(1);
                    return;
                }
            }
        }
    }
}
