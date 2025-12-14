using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace OldRetinues.Doctrines.Catalog
{
    public sealed class Captains : Doctrine
    {
        public override TextObject Name => L.T("captains", "Captains");
        public override TextObject Description => L.T("captains_description", "Unlocks Captains.");
        public override int Column => 2;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_MaxOutEliteAndBasic : Feat
        {
            public override TextObject Description =>
                L.T(
                    "captains_max_out_elite_basic",
                    "Max out the skills of a T6 elite troop and a T5 basic troop."
                );

            // Single combined objective: both conditions must be true.
            public override int Target => 1;

            public override void OnDailyTick()
            {
                // Already done, no need to keep scanning.
                if (Progress >= Target)
                    return;

                bool eliteDone = false;
                bool basicDone = false;

                foreach (var troop in Player.Troops)
                {
                    if (troop == null || !troop.IsValid)
                        continue;

                    // Tier 6 elite
                    if (
                        !eliteDone
                        && troop.IsElite
                        && troop.Tier >= 6
                        && SkillManager.SkillPointsLeft(troop) == 0
                    )
                        eliteDone = true;

                    // Tier 5 basic (non-elite)
                    if (
                        !basicDone
                        && !troop.IsElite
                        && troop.Tier >= 5
                        && SkillManager.SkillPointsLeft(troop) == 0
                    )
                        basicDone = true;

                    if (eliteDone && basicDone)
                        break;
                }

                if (eliteDone && basicDone)
                    SetProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_Earn200InfluenceInADay : Feat
        {
            public override TextObject Description =>
                L.T("captains_earn_200_influence_in_a_day", "Earn 200 influence in a single day.");

            public override int Target => 200;

            private int _previousInfluence;
            private bool _initialized;

            public override void OnRegister()
            {
                _previousInfluence = Player.Influence;
                _initialized = true;
            }

            public override void OnDailyTick()
            {
                if (!_initialized)
                {
                    _previousInfluence = Player.Influence;
                    _initialized = true;
                    return;
                }

                int current = Player.Influence;
                int gained = current - _previousInfluence;

                if (gained > Progress)
                    SetProgress(gained);

                _previousInfluence = current;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class CP_Have4000Renown : Feat
        {
            public override TextObject Description =>
                L.T("captains_have_4000_renown", "Have 4 000 renown.");

            public override int Target => 4000;

            public override void OnDailyTick()
            {
                int renown = (int)Player.Renown;

                if (renown > Progress)
                    SetProgress(renown);
            }
        }
    }
}
