using System;
using Retinues.Module;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mariner Flag                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsMarinerAttribute =>
            Attribute(
                getter: _ => NavalTraitHelper.GetMarinerLevel(Base) > 0,
                setter: (_, value) => NavalTraitHelper.SetMarinerLevel(Base, value ? 1 : 0),
                persistent: true
            );

        public bool IsMariner
        {
            get => IsMarinerAttribute.Get();
            set => IsMarinerAttribute.Set(value);
        }

        private static class NavalTraitHelper
        {
            private static TraitObject _navalSoldierTrait;
            private static bool _navalTraitMissing;

            private static TraitObject TryGetNavalSoldierTrait()
            {
                if (_navalTraitMissing)
                    return null;

                if (_navalSoldierTrait != null)
                    return _navalSoldierTrait;

                try
                {
                    // Resolve DefaultTraits by string so we don't rely on compile-time members.
                    var defaultTraitsType = Type.GetType(
                        "TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits, TaleWorlds.CampaignSystem",
                        throwOnError: false
                    );

                    if (defaultTraitsType == null)
                    {
                        _navalTraitMissing = true;
                        return null;
                    }

                    // Look for the static property "NavalSoldier" if it exists.
                    var prop = defaultTraitsType.GetProperty("NavalSoldier", Reflection.Flags);
                    if (prop == null)
                    {
                        _navalTraitMissing = true;
                        return null;
                    }

                    var value = prop.GetValue(null) as TraitObject;
                    if (value == null)
                        return null; // probably Campaign not ready yet; try again later

                    _navalSoldierTrait = value;
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
                }
                catch { }
            }
        }
    }
}
