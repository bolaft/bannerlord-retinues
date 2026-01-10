using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Factions.Base;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WClan(Clan @base) : BaseMapFaction<WClan, Clan>(@base)
    {
        // Clan is mostly campaign-managed. During early load Campaign.Current may be null.
        // Prefer MBObjectManager if possible, then fallback to Campaign.Current lists.

        public static new WClan Get(string stringId)
        {
            Log.Info($"WClan.Get('{stringId}') called.");
            if (string.IsNullOrEmpty(stringId))
                return null;

            // 1) Try MBObjectManager first (often available earlier than Campaign.Current).
            var mgr = MBObjectManager.Instance;
            var mbo = mgr?.GetObject<Clan>(stringId);
            Log.Info(
                $"  MBObjectManager lookup returned {(mbo == null ? "null" : mbo.Name.ToString())}."
            );
            if (mbo != null)
                return Get(mbo);

            // 2) Fallback to campaign list when available.
            var campaign = Campaign.Current;
            var clans = campaign?.Clans;
            Log.Info(
                $"  Campaign lookup returned {(clans == null ? "null" : $"{clans.Count} clans")}."
            );
            if (clans == null)
                return null;

            var clan = clans.FirstOrDefault(c => c != null && c.StringId == stringId);
            Log.Info($"  Campaign search found {(clan == null ? "null" : clan.Name.ToString())}.");
            return clan == null ? null : Get(clan);
        }

        public static new IEnumerable<WClan> All
        {
            get
            {
                // 1) MBObjectManager list if available
                var mgr = MBObjectManager.Instance;
                var list = mgr?.GetObjectTypeList<Clan>();
                Log.Info(
                    $"WClan.All called. MBObjectManager returned {(list == null ? "null" : $"{list.Count} clans")}."
                );
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var c = list[i];
                        if (c == null)
                            continue;

                        var w = Get(c);
                        if (w != null)
                            yield return w;
                    }

                    yield break;
                }

                // 2) Campaign list fallback
                var campaign = Campaign.Current;
                var clans = campaign?.Clans;
                Log.Info(
                    $"WClan.All: Campaign lookup returned {(clans == null ? "null" : $"{clans.Count} clans")}."
                );
                if (clans == null)
                    yield break;

                for (int i = 0; i < clans.Count; i++)
                {
                    var c = clans[i];
                    if (c == null)
                        continue;

                    var w = Get(c);
                    if (w != null)
                        yield return w;
                }
            }
        }
    }
}
