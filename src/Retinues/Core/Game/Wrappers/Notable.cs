using System;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WNotable(Hero notable) : WHero(notable)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SwapVolunteers(WFaction faction)
        {
            if (Base == null || faction == null)
                return;

            var arr = Base.VolunteerTypes;
            if (arr == null || arr.Length == 0)
                return;

            for (int i = 0; i < arr.Length; i++)
            {
                var vanilla = arr[i];

                // Preserve empty slots
                if (vanilla == null)
                    continue;

                var troop = new WCharacter(vanilla);

                if (!troop.IsValid)
                {
                    arr[i] = null; // Fix corrupt entries
                    continue;
                }

                if (troop.Faction == faction)
                    continue; // Already correct faction troop

                var replacement = TroopMatcher.PickBestFromFaction(faction, troop);

                if (replacement != null)
                    arr[i] = replacement.Base;
            }
        }
    }
}
