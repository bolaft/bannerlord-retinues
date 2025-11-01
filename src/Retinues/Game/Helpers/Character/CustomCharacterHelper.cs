using System.Linq;
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

        public bool IsRetinue(WCharacter node) => Player.RetinueTroops.Contains(node);

        public bool IsMilitiaMelee(WCharacter node) => Player.MilitiaMeleeTroops.Contains(node);

        public bool IsMilitiaRanged(WCharacter node) => Player.MilitiaRangedTroops.Contains(node);

        public bool IsElite(WCharacter node) => Player.EliteTroops.Contains(node);

        public bool IsKingdom(WCharacter node) => Player.Kingdom.Troops.Contains(node);

        public bool IsClan(WCharacter node) => Player.Clan.Troops.Contains(node);

        public WFaction ResolveFaction(WCharacter node) =>
            Player.Clan.Troops.Contains(node) ? Player.Clan
            : Player.Kingdom.Troops.Contains(node) ? Player.Kingdom
            : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Wrapper Convenience                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter GetParent(WCharacter node)
        {
            var faction = ResolveFaction(node);

            foreach (var troop in faction.BasicTroops.Concat(faction.EliteTroops))
                if (troop.UpgradeTargets.Contains(node))
                    return troop;

            return null;
        }
    }
}
