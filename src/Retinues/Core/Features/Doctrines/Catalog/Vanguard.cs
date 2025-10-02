using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Game;
using Retinues.Core.Game.Events;
using Retinues.Core.Utils;
using TaleWorlds.Localization;

namespace Retinues.Core.Features.Doctrines.Catalog
{
    public sealed class Vanguard : Doctrine
    {
        public override TextObject Name => L.T("vanguard", "Vanguard");
        public override TextObject Description => L.T("vanguard_description", "+15% retinue cap.");
        public override int Column => 3;
        public override int Row => 2;

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
                    // Heuristic: if any kill is not by a retinue in player troop, disqualify
                    if (kill.Victim.Side == battle.PlayerSide)
                    {
                        if (!kill.Victim.Character.IsRetinue && !kill.Victim.IsPlayer)
                            return; // A non-retinue / non-player was present
                    }
                    else if (!kill.Killer.Character.IsRetinue)
                    {
                        if (!kill.Killer.IsPlayer)
                            return; // A non-retinue / non-player was present
                    }
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
                    if (kill.Blow.IsMissile)
                        continue; // Ignore ranged kills
                    if (!kill.Killer.IsPlayerTroop)
                        return; // First melee kill not by player troop
                    if (!kill.Killer.Character.IsRetinue)
                        return; // First melee kill not by retinue

                    AdvanceProgress(1);
                    return;
                }
            }
        }
    }
}
