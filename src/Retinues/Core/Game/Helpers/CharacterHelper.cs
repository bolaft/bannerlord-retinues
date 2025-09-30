using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Game.Helpers
{
    public static class CharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Reflection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly FieldInfo F_originCharacter = AccessTools.Field(
            typeof(CharacterObject),
            "_originCharacter"
        );
        private static readonly FieldInfo F_occupation = AccessTools.Field(
            typeof(CharacterObject),
            "_occupation"
        );
        private static readonly FieldInfo F_persona = AccessTools.Field(
            typeof(CharacterObject),
            "_persona"
        );
        private static readonly FieldInfo F_characterTraits = AccessTools.Field(
            typeof(CharacterObject),
            "_characterTraits"
        );
        private static readonly FieldInfo F_civilianEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_civilianEquipmentTemplate"
        );
        private static readonly FieldInfo F_battleEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_battleEquipmentTemplate"
        );
        private static readonly MethodInfo M_fillFrom = AccessTools.Method(
            typeof(CharacterObject),
            "FillFrom",
            [typeof(CharacterObject)]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Copy                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static CharacterObject CopyInto(CharacterObject src, CharacterObject tgt)
        {
            // origin
            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            // Hero block — same logic as the original (usually false for troops)
            if (tgt.IsHero)
            {
                // var staticProps = src.IsHero
                //     ? src.HeroObject.StaticBodyProperties
                //     : src.GetBodyPropertiesMin().StaticProperties;
                // tgt.HeroObject.StaticBodyProperties = staticProps;
            }

            // Copy the fields the engine copies
            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (CharacterTraits)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(
                tgt,
                traitsSrc != null ? new CharacterTraits(traitsSrc) : null
            );

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            // Fill the rest
            M_fillFrom.Invoke(tgt, [src]);

            return tgt;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Identifiers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string BuildId(
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

        // Turns a path (e.g., [], [0], [1,0]) into our token string.
        private static string BuildTokenForPath(IReadOnlyList<int> path)
        {
            if (path == null || path.Count == 0)
                return "r"; // root => "r"
            return string.Concat(path.Select(i => i == 0 ? '0' : '1')); // branch => "01..."
        }

        public static CharacterObject GetCharacterObject(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path = null
        )
        {
            var id = BuildId(isKingdom, isElite, isRetinue, isMilitiaMelee, isMilitiaRanged, path);
            return GetCharacterObject(id);
        }

        public static CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Parse Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsCustom(string id) => id?.Contains("ret_") == true;
        public static bool IsRetinue(string id) => ExtractToken(id) == "retinue";
        public static bool IsMilitiaMelee(string id) => ExtractToken(id) == "mmilitia";
        public static bool IsMilitiaRanged(string id) => ExtractToken(id) == "rmilitia";
        public static bool IsElite(string id) => id?.Contains("_elite_") == true;
        public static bool IsKingdom(string id) => id?.Contains("_kingdom_") == true;
        public static bool IsClan(string id) => id?.Contains("_clan_") == true;

        public static IReadOnlyList<int> GetPath(string id)
        {
            var token = ExtractToken(id);
            if (string.IsNullOrEmpty(token) || token == "retinue")
                return [];
            if (token == "r")
                return []; // root

            // Binary path "01..." => [0,1,...]
            var path = new List<int>(token.Length);
            foreach (var ch in token)
            {
                if (ch == '0')
                    path.Add(0);
                else if (ch == '1')
                    path.Add(1);
                else
                    break; // not a path (shouldn't happen with our IDs)
            }
            return path;
        }

        public static WFaction ResolveFaction(string id)
        {
            if (IsKingdom(id))
                return Player.Kingdom;
            return IsClan(id) ? Player.Clan : null;
        }

        public static string GetParentId(string id)
        {
            if (IsRetinue(id))
                return null; // retinues are leaves
            var path = GetPath(id);
            if (path.Count == 0)
                return null; // already root
            var parentPath = path.Take(path.Count - 1).ToList();
            return BuildId(IsKingdom(id), IsElite(id), isRetinue: false, isMilitiaMelee: false, isMilitiaRanged: false, parentPath);
        }

        public static IEnumerable<string> GetChildrenIds(string id)
        {
            if (IsRetinue(id))
                yield break; // leaves
            var path = GetPath(id);
            // Two branches (0 & 1) — stubs must exist in xml for these to be valid
            yield return BuildId(
                IsKingdom(id),
                IsElite(id),
                isRetinue: false,
                isMilitiaMelee: false, isMilitiaRanged: false,
                path.Concat([0]).ToList()
            );
            yield return BuildId(
                IsKingdom(id),
                IsElite(id),
                isRetinue: false,
                isMilitiaMelee: false, isMilitiaRanged: false,
                path.Concat([1]).ToList()
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter GetParent(WCharacter node)
        {
            if (node.IsRetinue || node.IsMilitiaMelee || node.IsMilitiaRanged)
                return null;  // leaves
            if (node == null)
                return null;
            var pid = GetParentId(node.StringId);
            if (string.IsNullOrEmpty(pid))
                return null;
            var pco = GetCharacterObject(pid);
            return pco != null ? new WCharacter(pco) : null;
        }

        public static IEnumerable<WCharacter> GetChildren(WCharacter node)
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
