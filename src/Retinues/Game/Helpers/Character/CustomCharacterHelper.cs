using Retinues.Game.Wrappers;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for custom troops.
    /// </summary>
    public sealed class CustomCharacterHelper : CharacterHelperBase, ICharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsRetinue(WCharacter node) => CharacterGraphIndex.IsRetinue(node);

        public bool IsMilitiaMelee(WCharacter node) => CharacterGraphIndex.IsMilitiaMelee(node);

        public bool IsMilitiaRanged(WCharacter node) => CharacterGraphIndex.IsMilitiaRanged(node);

        public bool IsCaravanGuard(WCharacter node) => CharacterGraphIndex.IsCaravanGuard(node);

        public bool IsCaravanMaster(WCharacter node) => CharacterGraphIndex.IsCaravanMaster(node);

        public bool IsVillager(WCharacter node) => CharacterGraphIndex.IsVillager(node);

        public bool IsElite(WCharacter node) => CharacterGraphIndex.IsElite(node);

        public bool IsKingdom(WCharacter node) =>
            CharacterGraphIndex.TryGetFaction(node) == Player.Kingdom;

        public bool IsClan(WCharacter node) =>
            CharacterGraphIndex.TryGetFaction(node) == Player.Clan;

        public WFaction ResolveFaction(WCharacter node) => CharacterGraphIndex.TryGetFaction(node);

        public WCharacter GetParent(WCharacter node) => CharacterGraphIndex.TryGetParent(node);
    }
}
