using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for custom troops. Handles ID building, parsing, graph navigation, and wrapper convenience for custom troop logic.
    /// </summary>
    public sealed class CustomCharacterHelper : CharacterHelperBase, ICharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Build / Resolve                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string AllocateId() => TroopIndex.AllocateStub();

        public CharacterObject GetCharacterObject(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path = null
        )
        {
            // 1) Try reuse: exact signature (flags + path)
            var existing = TroopIndex.FindBySignature(isKingdom, isElite, isRetinue, isMilitiaMelee, isMilitiaRanged, path);
            if (existing != null)
            {
                var co0 = MBObjectManager.Instance.GetObject<CharacterObject>(existing.Id);
                if (co0 != null) return co0;
                // fall through to allocate if somehow missing (shouldn't happen if stubs are loaded)
            }

            // 2) Allocate a fresh stub
            var id = AllocateId();
            if (string.IsNullOrEmpty(id))
                return null;

            TroopIndex.SetFlags(id, isKingdom, isElite, isRetinue, isMilitiaMelee, isMilitiaRanged);

            // 3) Parent relation from path (parent = path[:-1])
            if (path != null && path.Count > 0)
            {
                var parentPath = path.Take(path.Count - 1).ToArray();
                var parent = TroopIndex.FindByPath(parentPath);
                TroopIndex.SetParent(id, parent?.Id, path[path.Count - 1]);
            }
            else
            {
                TroopIndex.SetParent(id, null, 0);
            }

            // 4) Return the stub object (pre-registered by your XML)
            return MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        public CharacterObject GetCharacterObject(string id) =>
            TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>(id);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCustom(string id) =>
            id != null && (id.Contains("ret_") || id.StartsWith("retinues_custom_"));

        public bool IsRetinue(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) && e.IsRetinue;

        public bool IsMilitiaMelee(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) && e.IsMilitiaMelee;

        public bool IsMilitiaRanged(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) && e.IsMilitiaRanged;

        public bool IsElite(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) && e.IsElite;

        public bool IsKingdom(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) && e.IsKingdom;

        public bool IsClan(string id) => !IsKingdom(id);

        public IReadOnlyList<int> GetPath(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) ? e.Path ?? [] : [];

        public WFaction ResolveFaction(string id) => IsKingdom(id) ? Player.Kingdom : Player.Clan;

        public string GetParentId(string id) =>
            TroopBehavior.Index.TryGetValue(id, out var e) ? e.ParentId : null;

        public IEnumerable<string> GetChildrenIds(string id) => TroopIndex.GetChildrenIds(id);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Wrapper Convenience                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter GetParent(WCharacter node)
        {
            var pid = GetParentId(node?.StringId);
            if (string.IsNullOrEmpty(pid))
                return null;
            var pco = GetCharacterObject(pid);
            return pco != null ? new WCharacter(pco) : null;
        }

        public IEnumerable<WCharacter> GetChildren(WCharacter node)
        {
            if (node == null)
                yield break;
            foreach (var cid in GetChildrenIds(node.StringId))
            {
                var co = GetCharacterObject(cid);
                if (co != null)
                    yield return new WCharacter(co);
            }
        }
    }
}
