using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Game.Wrappers.Cache
{
    public static class WCharacterCache
    {
        private static readonly ConditionalWeakTable<CharacterObject, WCharacter> _byObj = new();

        // Optional: fast maps so we can seed faction/parent (see #2 below)
        public static WFaction GetFactionFor(string troopId) =>
            WCharacterIndex.TryGetFactionByTroopId(troopId, out var f) ? f : null;

        public static WCharacter Wrap(CharacterObject co)
        {
            return _byObj.GetValue(co, key =>
            {
                // seed from index if available
                var fac = GetFactionFor(key.StringId);
                WCharacter parent = null;
                if (WCharacterIndex.TryGetParentId(key.StringId, out var parentId))
                {
                    var pObj = MBObjectManager.Instance.GetObject<CharacterObject>(parentId);
                    if (pObj != null) parent = Wrap(pObj);
                }
                return new WCharacter(key, fac, parent);
            });
        }

        public static void Clear() { /* optional: nothing to do for CWT */ }
    }
}
