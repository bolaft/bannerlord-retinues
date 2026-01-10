using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Factions.Base;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Factions.Wrappers
{
    public sealed class WKingdom(Kingdom @base) : BaseMapFaction<WKingdom, Kingdom>(@base)
    {
        public static new WKingdom Get(string stringId)
        {
            if (string.IsNullOrEmpty(stringId))
                return null;

            var mgr = MBObjectManager.Instance;
            var mbo = mgr?.GetObject<Kingdom>(stringId);
            if (mbo != null)
                return Get(mbo);

            var campaign = Campaign.Current;
            var kingdoms = campaign?.Kingdoms;
            if (kingdoms == null)
                return null;

            var k = kingdoms.FirstOrDefault(x => x != null && x.StringId == stringId);
            return k == null ? null : Get(k);
        }

        public static new IEnumerable<WKingdom> All
        {
            get
            {
                var mgr = MBObjectManager.Instance;
                var list = mgr?.GetObjectTypeList<Kingdom>();
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var k = list[i];
                        if (k == null)
                            continue;

                        var w = Get(k);
                        if (w != null)
                            yield return w;
                    }

                    yield break;
                }

                var campaign = Campaign.Current;
                var kingdoms = campaign?.Kingdoms;
                if (kingdoms == null)
                    yield break;

                for (int i = 0; i < kingdoms.Count; i++)
                {
                    var k = kingdoms[i];
                    if (k == null)
                        continue;

                    var w = Get(k);
                    if (w != null)
                        yield return w;
                }
            }
        }
    }
}
