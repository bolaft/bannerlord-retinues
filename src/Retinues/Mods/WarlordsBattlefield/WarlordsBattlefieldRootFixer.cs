using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace Retinues.Mods.WarlordsBattlefield
{
    public sealed class WarlordsBattlefieldRootFixer
    {
        public static WCharacter FixRoot(WCharacter root, bool isElite)
        {
            if (root == null)
                return null;

            int tier = isElite ? 2 : 1;

            // Custom fix for Warlords Battlefield
            foreach (var troop in root.Tree)
            {
                if (troop.Tier == tier)
                {
                    Log.Info($"Using Warlords Battlefield elite root {troop.Name} ({troop})");
                    return troop;
                }
            }

            return root;
        }
    }
}
