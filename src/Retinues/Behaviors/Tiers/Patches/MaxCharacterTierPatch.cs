using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem.GameComponents;

namespace Retinues.Behaviors.Tiers.Patches
{
    /// <summary>
    /// Raises the engine's character-tier ceiling to the configured maximum so custom troops can
    /// rank up past the vanilla cap of 6.
    ///
    /// CharacterObject.Tier is computed as clamp(ceil((Level-5)/5), 0, MaxCharacterTier), so the
    /// editor's higher tiers would be inert without lifting MaxCharacterTier. We take the higher of
    /// the existing value and the configured cap, so this composes with other tier-unlocker mods
    /// rather than fighting them. A no-op at the default cap of 6.
    /// </summary>
    [HarmonyPatch(typeof(DefaultCharacterStatsModel), "MaxCharacterTier", MethodType.Getter)]
    internal static class MaxCharacterTierPatch
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
