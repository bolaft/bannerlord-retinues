using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;

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
            var id = AllocateId();
            if (string.IsNullOrEmpty(id))
                return null;

            // Persist flags + relations immediately
            TroopIndex.SetFlags(id, isKingdom, isElite, isRetinue, isMilitiaMelee, isMilitiaRanged);

            // Parent relation if a path is supplied: parent is path[:-1]
            if (path != null && path.Count > 0)
            {
                // Find parent by exact path prefix in the index
                var parent = TroopBehavior.Index.Values.FirstOrDefault(e =>
                    e.Path != null && e.Path.SequenceEqual(path.Take(path.Count - 1))
                );
                TroopIndex.SetParent(id, parent?.Id, path[path.Count - 1]);
            }
            else
            {
                TroopIndex.SetParent(id, null, 0);
            }

            return TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>(id);
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
            TroopBehavior.Index.TryGetValue(id, out var e)
                ? (IReadOnlyList<int>)(e.Path ?? new List<int>())
                : new List<int>();

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
