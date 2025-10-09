using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Interface for character helpers, provides methods for ID lookup, parsing, graph navigation, and deep copy.
    /// Used to abstract custom/vanilla troop logic.
    /// </summary>
    public interface ICharacterHelper
    {
        /* ━ ID Building / Lookup ━ */

        CharacterObject GetCharacterObject(string id);
        CharacterObject GetCharacterObject(
            bool isKingdom,
            bool isElite,
            bool isRetinue,
            bool isMilitiaMelee,
            bool isMilitiaRanged,
            IReadOnlyList<int> path = null
        );

        /* ━━━━━━ ID Parsing ━━━━━━ */

        bool IsCustom(string id);
        bool IsRetinue(string id);
        bool IsMilitiaMelee(string id);
        bool IsMilitiaRanged(string id);
        bool IsElite(string id);
        bool IsKingdom(string id);
        bool IsClan(string id);
        IReadOnlyList<int> GetPath(string id);
        WFaction ResolveFaction(string id);

        /* ━━━ Graph Navigation ━━━ */

        string GetParentId(string id);
        IEnumerable<string> GetChildrenIds(string id);

        /* ━━ Wrapper Convenience ━ */

        WCharacter GetParent(WCharacter node);
        IEnumerable<WCharacter> GetChildren(WCharacter node);

        /* ━━━━━━━ Deep Copy ━━━━━━ */

        CharacterObject CopyInto(CharacterObject src, CharacterObject tgt);
    }
}
