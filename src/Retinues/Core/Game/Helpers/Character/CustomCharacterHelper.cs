using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Helpers.Character
{
    public sealed class CustomCharacterHelper : CharacterHelperBase, ICharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Build / Resolve                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string BuildId(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path
        )
        {
            var scope = isKingdom ? "kingdom" : "clan";
            var kind = isElite ? "elite" : "basic";

            string token;
            if (isRetinue)
                token = "retinue";
            else if (isMilitiaMelee)
                token = "mmilitia";
            else if (isMilitiaRanged)
                token = "rmilitia";
            else
                token = BuildTokenForPath(path);

            return $"ret_{scope}_{kind}_{token}";
        }

        public CharacterObject GetCharacterObject(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path = null
        ) =>
            GetCharacterObject(
                BuildId(isKingdom, isElite, isRetinue, isMilitiaMelee, isMilitiaRanged, path)
            );

        public CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsCustom(string id) => id != null && id.Contains("ret_");

        public bool IsRetinue(string id) => ExtractToken(id) == "retinue";

        public bool IsMilitiaMelee(string id) => ExtractToken(id) == "mmilitia";

        public bool IsMilitiaRanged(string id) => ExtractToken(id) == "rmilitia";

        public bool IsElite(string id) => id != null && id.Contains("_elite_");

        public bool IsKingdom(string id) => id != null && id.Contains("_kingdom_");

        public bool IsClan(string id) => id != null && id.Contains("_clan_");

        public IReadOnlyList<int> GetPath(string id)
        {
            var token = ExtractToken(id);
            if (string.IsNullOrEmpty(token) || token == "retinue")
                return [];
            if (token == "r")
                return [];

            var path = new List<int>(token.Length);
            foreach (var ch in token)
            {
                if (ch == '0')
                    path.Add(0);
                else if (ch == '1')
                    path.Add(1);
                else
                    break;
            }
            return path;
        }

        public WFaction ResolveFaction(string id)
        {
            if (IsKingdom(id))
                return Player.Kingdom;
            return IsClan(id) ? Player.Clan : null;
        }

        public string GetParentId(string id)
        {
            if (IsRetinue(id))
                return null; // retinues are leaves

            var path = GetPath(id);
            if (path.Count == 0)
                return null; // already root

            var parentPath = path.Take(path.Count - 1).ToList();
            return BuildId(
                IsKingdom(id),
                IsElite(id),
                isRetinue: false,
                isMilitiaMelee: false,
                isMilitiaRanged: false,
                parentPath
            );
        }

        public IEnumerable<string> GetChildrenIds(string id)
        {
            if (IsRetinue(id))
                yield break;

            var path = GetPath(id);

            yield return BuildId(
                IsKingdom(id),
                IsElite(id),
                isRetinue: false,
                isMilitiaMelee: false,
                isMilitiaRanged: false,
                [.. path, .. new[] { 0 }]
            );

            yield return BuildId(
                IsKingdom(id),
                IsElite(id),
                isRetinue: false,
                isMilitiaMelee: false,
                isMilitiaRanged: false,
                [.. path, .. new[] { 1 }]
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Wrapper Convenience                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter GetParent(WCharacter node)
        {
            if (node == null || node.IsRetinue || node.IsMilitiaMelee || node.IsMilitiaRanged)
                return null;

            var pid = GetParentId(node.StringId);
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string BuildTokenForPath(IReadOnlyList<int> path)
        {
            if (path == null || path.Count == 0)
                return "r";
            return string.Concat(path.Select(i => i == 0 ? '0' : '1'));
        }

        private static string ExtractToken(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            var underscore = id.LastIndexOf('_');
            return underscore >= 0 && underscore + 1 < id.Length
                ? id.Substring(underscore + 1)
                : null;
        }
    }
}
