using System;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Safety
{
    [SafeClass]
    public static class VolunteerSanitizer
    {
        public static void CleanSettlement(Settlement settlement)
        {
            if (settlement == null)
                return;

            foreach (var notable in settlement.Notables)
            {
                if (notable == null)
                    continue;

                SwapNotable(notable, settlement);
            }
        }

        private static void SwapNotable(Hero notable, Settlement settlement)
        {
            if (notable?.VolunteerTypes == null)
                return;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                try
                {
                    var c = notable.VolunteerTypes[i];

                    if (c == null)
                        continue;

                    if (IsCharacterValid(c))
                        continue;

                    var fallback = GetFallbackVolunteer(settlement);
                    if (fallback != null)
                    {
                        Log.Warn(
                            $"[VolunteerSanitizer] Replacing invalid volunteer at [{settlement?.Name}] "
                                + $"notable '{notable?.Name}' slot {i} "
                                + $"('{c?.StringId ?? "NULL"}' -> '{fallback.StringId}')."
                        );
                        notable.VolunteerTypes[i] = fallback;
                    }
                    else
                    {
                        Log.Warn(
                            $"[VolunteerSanitizer] Removing invalid volunteer at [{settlement?.Name}] "
                                + $"notable '{notable?.Name}' slot {i} ('{c?.StringId ?? "NULL"}')."
                        );
                        notable.VolunteerTypes[i] = null;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(
                        e,
                        $"[VolunteerSanitizer] Exception while processing notable '{notable?.Name}' in settlement '{settlement?.Name}'"
                    );
                }
            }
        }

        private static CharacterObject GetFallbackVolunteer(Settlement settlement)
        {
            CharacterObject pick = null;

            try
            {
                pick = settlement?.Culture?.BasicTroop;
            }
            catch { }

            if (pick == null)
            {
                pick = MBObjectManager.Instance?.GetObject<CharacterObject>("looter");
            }

            return IsCharacterValid(pick) ? pick : null;
        }

        private static bool IsCharacterValid(CharacterObject c)
        {
            if (c == null)
            {
                Log.Warn("IsCharacterValid: CharacterObject is null");
                return false;
            }

            var w = new WCharacter(c);

            if (!w.IsActive)
            {
                Log.Warn($"IsCharacterValid: Character '{c.StringId}' is not active");
                return false;
            }

            if (string.IsNullOrWhiteSpace(c.StringId))
            {
                Log.Warn("IsCharacterValid: StringId is null or whitespace");
                return false;
            }
            if (c.Name == null)
            {
                Log.Warn($"IsCharacterValid: Character '{c.StringId}' has null Name");
                return false;
            }
            if (c.Tier < 0 || c.Tier > 10)
            {
                Log.Warn($"IsCharacterValid: Character '{c.StringId}' has invalid Tier {c.Tier}");
                return false;
            }

            var fromDb = MBObjectManager.Instance?.GetObject<CharacterObject>(c.StringId);
            if (!ReferenceEquals(fromDb, c) && fromDb == null)
            {
                Log.Warn(
                    $"IsCharacterValid: Character '{c.StringId}' not found in MBObjectManager"
                );
                return false;
            }

            return true;
        }
    }
}
