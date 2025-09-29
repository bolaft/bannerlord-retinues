using System;
using TaleWorlds.CampaignSystem.Settlements;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Safety
{
    public static class VolunteerSanitizer
    {
        public static void CleanSettlement(Settlement settlement)
        {
            if (settlement == null) return;

            try
            {
                foreach (var notable in settlement.Notables)
                {
                    if (notable?.VolunteerTypes == null) continue;

                    for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                    {
                        try
                        {
                            var character = notable.VolunteerTypes[i];
                            if (character == null)
                                continue;

                            var wChar = new WCharacter(character);

                            if (wChar.IsCustom && !wChar.IsActive)
                            {
                                Log.Warn($"[VolunteerSanitizer] Removing inactive custom volunteer {wChar?.StringId} from notable {notable?.Name} ({settlement?.Name}).");
                                notable.VolunteerTypes[i] = null;
                            }
                        }
                        catch (Exception exInner)
                        {
                            Log.Exception(exInner, $"[VolunteerSanitizer] Failed cleaning volunteer index {i} for notable {notable?.Name} ({settlement?.Name})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[VolunteerSanitizer] Failed cleaning volunteers for {settlement?.Name}");
            }
        }
    }
}
