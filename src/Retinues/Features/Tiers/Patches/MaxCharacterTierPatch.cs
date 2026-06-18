using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.GameComponents;

namespace Retinues.Features.Tiers.Patches
{
    /// <summary>
    /// Raises the engine's character-tier ceiling to the configured maximum so custom troops can
    /// actually rank up past the vanilla cap of 6.
    ///
    /// CharacterObject.Tier is computed as clamp(ceil((Level-5)/5), 0, MaxCharacterTier), so the
    /// editor's higher tiers would be inert without lifting MaxCharacterTier. We take the higher of
    /// the existing value and our configured cap, so this composes with other tier-unlocker mods
    /// (TroopTierPlus, T7TroopUnlocker, ...) rather than fighting them. A no-op at the default cap.
    /// </summary>
    [HarmonyPatch(typeof(DefaultCharacterStatsModel), "MaxCharacterTier", MethodType.Getter)]
    public static class MaxCharacterTierPatch
    {
        [SafeMethod]
        static void Postfix(ref int __result)
        {
            int cap = WCharacter.EliteMaxTier;
            if (cap > __result)
                __result = cap;
        }
    }
}
