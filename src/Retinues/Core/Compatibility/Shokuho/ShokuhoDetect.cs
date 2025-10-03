using System;
using Retinues.Core.Utils;
using TaleWorlds.Core;

public static class ShokuhoDetect
{
    public static bool IsShokuhoCampaign()
    {
        try
        {
            var gt = Game.Current?.GameType;
            if (gt != null)
            {
                var t = gt.GetType();
                var name = t.Name; // e.g. "ShokuhoCampaign"
                var full = t.FullName ?? string.Empty;

                // Fast path: exact or obvious name match
                if (string.Equals(name, "ShokuhoCampaign", StringComparison.Ordinal))
                    return true;
                if (full.IndexOf("ShokuhoCampaign", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (full.IndexOf(".Shokuho.", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // Fallback 1: module is loaded (avoid crashing if API changes)
            try
            {
                foreach (var m in ModuleChecker.GetActiveModules())
                    if (string.Equals(m.Id, "Shokuho", StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            catch
            { /* ignore */
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
