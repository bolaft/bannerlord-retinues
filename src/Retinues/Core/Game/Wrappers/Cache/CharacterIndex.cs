using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Retinues.Core.Game.Wrappers.Cache
{
    public static class WCharacterIndex
    {
        private static readonly ConcurrentDictionary<string, WFaction> _factionByTroopId = new();
        private static readonly ConcurrentDictionary<string, string> _parentIdByChildId = new();

        public static void Rebuild()
        {
            _factionByTroopId.Clear();
            _parentIdByChildId.Clear();

            var sources = new List<WFaction> { Player.Clan };

            if (Player.Kingdom != null)
                sources.Add(Player.Kingdom);

            foreach (var fac in sources)
            {
                foreach (var t in fac.BasicTroops.Concat(fac.EliteTroops))
                {
                    _factionByTroopId[t.StringId] = fac;

                    foreach (var child in t.UpgradeTargets)
                        _parentIdByChildId[child.StringId] = t.StringId;
                }

                if (fac.RetinueBasic != null)
                    _factionByTroopId[fac.RetinueBasic.StringId] = fac;
                if (fac.RetinueElite != null)
                    _factionByTroopId[fac.RetinueElite.StringId] = fac;
            }
        }

        public static bool TryGetFactionByTroopId(string id, out WFaction fac) =>
            _factionByTroopId.TryGetValue(id, out fac);

        public static bool TryGetParentId(string id, out string parentId) =>
            _parentIdByChildId.TryGetValue(id, out parentId);

        public static void InvalidateTroop(string id)
        {
            _factionByTroopId.TryRemove(id, out _);
            _parentIdByChildId.TryRemove(id, out _);
        }
    }
}
