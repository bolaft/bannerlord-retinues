using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Helpers.Character;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety.Legacy
{
    /// <summary>
    /// Legacy helper for custom troops. Handles ID building, parsing, graph navigation, and wrapper convenience for custom troop logic.
    /// </summary>
    public sealed class LegacyCustomCharacterHelper : CharacterHelperBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       ID Mapping                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const string IdPrefix = "retinues_custom_";
        const string IdPrefixLegacy = "ret_";

        /// <summary>
        /// Maps a legacy troop ID to the new custom character ID, migrating if necessary.
        /// </summary>
        public static CharacterObject MapLegacyIdToNewCharacter(string legacyId)
        {
            if (
                string.IsNullOrEmpty(legacyId)
                || !legacyId.StartsWith(IdPrefixLegacy)
                || legacyId.StartsWith(IdPrefix)
            )
                return MBObjectManager.Instance?.GetObject<CharacterObject>(legacyId); // not legacy; return as-is

            var legacy = new LegacyCustomCharacterHelper();

            var isKingdom = legacy.IsKingdom(legacyId);
            var isElite = legacy.IsElite(legacyId);
            var isRetinue = legacy.IsRetinue(legacyId);
            var isMilitiaMelee = legacy.IsMilitiaMelee(legacyId);
            var isMilitiaRanged = legacy.IsMilitiaRanged(legacyId);
            var path = legacy.GetPath(legacyId)?.ToList() ?? new List<int>();

            // Reuse if already migrated
            var existing = TroopIndex.FindBySignature(
                isKingdom,
                isElite,
                isRetinue,
                isMilitiaMelee,
                isMilitiaRanged,
                path
            );
            string newId;
            if (existing != null)
            {
                newId = existing.Id;
            }
            else
            {
                newId = TroopIndex.AllocateStub();
                if (string.IsNullOrEmpty(newId))
                    return null;

                TroopIndex.SetFlags(
                    newId,
                    isKingdom,
                    isElite,
                    isRetinue,
                    isMilitiaMelee,
                    isMilitiaRanged
                );

                if (path.Count > 0)
                {
                    var parentPath = path.Take(path.Count - 1).ToArray();
                    var parent = TroopIndex.FindByPath(parentPath);
                    TroopIndex.SetParent(newId, parent?.Id, path[path.Count - 1]);
                }
                else
                {
                    TroopIndex.SetParent(newId, null, 0);
                }
            }

            return MBObjectManager.Instance?.GetObject<CharacterObject>(newId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Build / Resolve                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build a custom troop ID from flags and path.
        /// </summary>
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

        /// <summary>
        /// Get a CharacterObject by custom troop flags and path.
        /// </summary>
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

        /// <summary>
        /// Get a CharacterObject by custom troop ID.
        /// </summary>
        public CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the ID is a custom troop.
        /// </summary>
        public bool IsCustom(string id) => id != null && id.Contains("ret_");

        /// <summary>
        /// Returns true if the ID is a retinue troop.
        /// </summary>
        public bool IsRetinue(string id) => ExtractToken(id) == "retinue";

        /// <summary>
        /// Returns true if the ID is a melee militia troop.
        /// </summary>
        public bool IsMilitiaMelee(string id) => ExtractToken(id) == "mmilitia";

        /// <summary>
        /// Returns true if the ID is a ranged militia troop.
        /// </summary>
        public bool IsMilitiaRanged(string id) => ExtractToken(id) == "rmilitia";

        /// <summary>
        /// Returns true if the ID is an elite troop.
        /// </summary>
        public bool IsElite(string id) => id != null && id.Contains("_elite_");

        /// <summary>
        /// Returns true if the ID is a kingdom troop.
        /// </summary>
        public bool IsKingdom(string id) => id != null && id.Contains("_kingdom_");

        /// <summary>
        /// Returns true if the ID is a clan troop.
        /// </summary>
        public bool IsClan(string id) => id != null && id.Contains("_clan_");

        /// <summary>
        /// Gets the path (upgrade tree) for a custom troop ID.
        /// </summary>
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

        /// <summary>
        /// Resolves the faction for a custom troop ID.
        /// </summary>
        public WFaction ResolveFaction(string id)
        {
            if (IsKingdom(id))
                return Player.Kingdom;
            return IsClan(id) ? Player.Clan : null;
        }

        /// <summary>
        /// Gets the parent ID for a custom troop (null for retinues or roots).
        /// </summary>
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

        /// <summary>
        /// Gets the child IDs for a custom troop.
        /// </summary>
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

        /// <summary>
        /// Gets the parent troop for a node, or null if root/retinue/militia.
        /// </summary>
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

        /// <summary>
        /// Gets the child troops for a node.
        /// </summary>
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

        /// <summary>
        /// Builds the token string for a path.
        /// </summary>
        private static string BuildTokenForPath(IReadOnlyList<int> path)
        {
            if (path == null || path.Count == 0)
                return "r";
            return string.Concat(path.Select(i => i == 0 ? '0' : '1'));
        }

        /// <summary>
        /// Extracts the token from a custom troop ID.
        /// </summary>
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
