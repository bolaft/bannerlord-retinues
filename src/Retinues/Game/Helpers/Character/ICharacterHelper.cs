using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Interface for character helpers, provides methods for ID lookup, parsing, graph navigation, and deep copy.
    /// </summary>
    public interface ICharacterHelper
    {
        CharacterObject GetCharacterObject(string id);

        /* ━━━━━━ ID Parsing ━━━━━━ */

        WFaction ResolveFaction(WCharacter troop);

        bool IsRetinue(WCharacter troop);
        bool IsMilitiaMelee(WCharacter troop);
        bool IsMilitiaRanged(WCharacter troop);
        bool IsArmedTrader(WCharacter troop);
        bool IsCaravanGuard(WCharacter troop);
        bool IsCaravanMaster(WCharacter troop);
        bool IsVillager(WCharacter troop);
        bool IsElite(WCharacter troop);
        bool IsKingdom(WCharacter troop);
        bool IsClan(WCharacter troop);

        /* ━━━ Graph Navigation ━━━ */

        WCharacter GetParent(WCharacter node);

        /* ━━━━━━━ Deep Copy ━━━━━━ */

        CharacterObject CopyInto(CharacterObject src, CharacterObject tgt);
    }
}
