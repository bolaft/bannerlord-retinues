using System;
using Retinues.Compatibility;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Helpers
{
    /// <summary>
    /// Helpers for reading and writing the naval/mariner trait on character templates and heroes.
    /// </summary>
    public static class NavalTraitHelper
    {
        private static TraitObject _navalSoldierTrait;

        [StaticClearAction]
        /// <summary>
        /// Clears the cached naval soldier trait.
        /// </summary>
        public static void ClearCache()
        {
            _navalSoldierTrait = null;
        }

        private static TraitObject TryGetNavalSoldierTrait()
        {
            if (_navalSoldierTrait != null)
                return _navalSoldierTrait;

            // Use MBObjectManager directly — no Campaign.Current dependency.
            // Returns null transiently if not ready yet; caller retries next time.
            try
            {
                var trait = MBObjectManager.Instance?.GetObject<TraitObject>("NavalSoldier");
                if (trait == null)
                    return null;

                _navalSoldierTrait = trait;
                return _navalSoldierTrait;
            }
            catch
            {
                return null;
            }
        }

        private static object TryGetCharacterTraitsOwner(CharacterObject co)
        {
            if (co == null)
                return null;

            if (!Reflection.HasField(co, "_characterTraits"))
                return null;

            try
            {
                return Reflection.GetFieldValue<object>(co, "_characterTraits");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the mariner trait level for the given CharacterObject (hero or template).
        /// </summary>
        public static int GetMarinerLevel(CharacterObject co)
        {
            if (co == null)
                return 0;

            if (Mods.NavalDLC.IsLoaded == false)
                return 0; // No naval DLC, no mariner level

            var navalTrait = TryGetNavalSoldierTrait();
            if (navalTrait == null)
                return 0;

            // CharacterObject.GetTraitLevel handles both hero & non-hero.
            try
            {
                return co.GetTraitLevel(navalTrait);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Sets the mariner trait level on a CharacterObject (uses hero API when available).
        /// </summary>
        public static void SetMarinerLevel(CharacterObject co, int level)
        {
            if (co == null)
                return;

            if (!Mods.NavalDLC.IsLoaded)
                return; // No naval DLC, no mariner level

            var navalTrait = TryGetNavalSoldierTrait();
            if (navalTrait == null)
                return;

            level = Math.Max(0, level);

            // Hero case: use Hero.SetTraitLevel if available.
            if (co.IsHero && co.HeroObject != null)
            {
                try
                {
                    co.HeroObject.SetTraitLevel(navalTrait, level);
                    return;
                }
                catch
                {
                    // fall through to non-hero path if something weird happens
                }
            }

            // Non-hero template: poke CharacterObject._characterTraits via reflection.
            try
            {
                var owner = TryGetCharacterTraitsOwner(co);
                if (owner == null)
                    return;

                Reflection.InvokeMethod(
                    owner,
                    "SetPropertyValue",
                    parameterTypes: [typeof(TraitObject), typeof(int)],
                    navalTrait,
                    level
                );

                // Also update the IsMariner auto-property so the encyclopedia
                // and other UI that read character.IsMariner directly see the
                // correct value (CampaignUIHelper.GetCharacterTypeData reads it).
                Reflection.SetPropertyValue(co, "IsMariner", level > 0);
            }
            catch { }
        }
    }
}
