using System;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Domain.Events.Helpers
{
    /// <summary>
    /// Detects naval battles using reflection so we do not depend on NavalDLC.
    /// </summary>
    public static class NavalBattleHelper
    {
        public static bool IsNavalBattle(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return false;

            // 1) Direct property on MapEvent (most likely if the DLC adds it).
            if (TryGetBool(mapEvent, "IsNavalBattle", out var b1))
                return b1;

            // 2) Alternative names (defensive).
            if (TryGetBool(mapEvent, "IsNaval", out var b2))
                return b2;

            if (TryGetBool(mapEvent, "NavalBattle", out var b3))
                return b3;

            // 3) Heuristic fallback: any involved party looks naval.
            try
            {
                return SideHasNavalPartyHeuristic(mapEvent.DefenderSide)
                    || SideHasNavalPartyHeuristic(mapEvent.AttackerSide);
            }
            catch
            {
                return false;
            }
        }

        static bool TryGetBool(object instance, string propertyName, out bool value)
        {
            value = false;

            if (!Reflection.HasProperty(instance, propertyName))
                return false;

            try
            {
                var v = Reflection.GetPropertyValue(instance, propertyName);
                if (v is bool b)
                {
                    value = b;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static bool SideHasNavalPartyHeuristic(MapEventSide side)
        {
            if (side == null)
                return false;

            foreach (var p in side.Parties)
            {
                var mp = p?.Party?.MobileParty;
                if (mp == null)
                    continue;

                // Check component type name, no hard dep.
                var pc = mp.PartyComponent;
                if (pc != null)
                {
                    var name = pc.GetType().Name ?? "";
                    if (name.IndexOf("Naval", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                // Sometimes DLCs hang flags on the party itself.
                if (TryGetBool(mp, "IsNaval", out var b1) && b1)
                    return true;

                if (TryGetBool(mp, "IsNavalParty", out var b2) && b2)
                    return true;
            }

            return false;
        }
    }
}
