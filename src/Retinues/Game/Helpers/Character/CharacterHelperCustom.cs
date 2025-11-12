using Retinues.Game.Wrappers;

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Helper for custom troops.
    /// </summary>
    public sealed class CharacterHelperCustom : CharacterHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Public API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override bool IsRetinue(WCharacter node) => CharacterIndexer.IsRetinue(node);

        public override bool IsMilitiaMelee(WCharacter node) =>
            CharacterIndexer.IsMilitiaMelee(node);

        public override bool IsMilitiaRanged(WCharacter node) =>
            CharacterIndexer.IsMilitiaRanged(node);

        public override bool IsCaravanGuard(WCharacter node) =>
            CharacterIndexer.IsCaravanGuard(node);

        public override bool IsCaravanMaster(WCharacter node) =>
            CharacterIndexer.IsCaravanMaster(node);

        public override bool IsVillager(WCharacter node) => CharacterIndexer.IsVillager(node);

        public override bool IsElite(WCharacter node) => CharacterIndexer.IsElite(node);

        public override bool IsKingdom(WCharacter node) =>
            CharacterIndexer.TryGetFaction(node) == Player.Kingdom;

        public override bool IsClan(WCharacter node) =>
            CharacterIndexer.TryGetFaction(node) == Player.Clan;

        public override WFaction ResolveFaction(WCharacter node) =>
            CharacterIndexer.TryGetFaction(node);

        public override WCharacter GetParent(WCharacter node) =>
            CharacterIndexer.TryGetParent(node);
    }
}
