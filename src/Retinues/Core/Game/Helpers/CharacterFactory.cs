using HarmonyLib;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using System;
using System.Reflection;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Helpers
{
    public static class CharacterFactory
    {
        private static readonly FieldInfo F_originCharacter =
            AccessTools.Field(typeof(CharacterObject), "_originCharacter");
        private static readonly FieldInfo F_occupation =
            AccessTools.Field(typeof(CharacterObject), "_occupation");
        private static readonly FieldInfo F_persona =
            AccessTools.Field(typeof(CharacterObject), "_persona");
        private static readonly FieldInfo F_characterTraits =
            AccessTools.Field(typeof(CharacterObject), "_characterTraits");
        private static readonly FieldInfo F_civilianEquipmentTemplate =
            AccessTools.Field(typeof(CharacterObject), "_civilianEquipmentTemplate");
        private static readonly FieldInfo F_battleEquipmentTemplate =
            AccessTools.Field(typeof(CharacterObject), "_battleEquipmentTemplate");
        private static readonly MethodInfo M_fillFrom =
            AccessTools.Method(typeof(CharacterObject), "FillFrom", [typeof(CharacterObject)]);

        public static CharacterObject CloneWithId(CharacterObject src, string stringId)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (string.IsNullOrEmpty(stringId)) throw new ArgumentException("stringId required", nameof(stringId));

            Log.Debug($"Cloning CharacterObject: {src.StringId} to {stringId}");

            // Create with your ID
            var clone = MBObjectManager.Instance.CreateObject<CharacterObject>(stringId);

            clone = CopyCharacterObject(src, clone);

            return clone;
        }

        private static CharacterObject CopyCharacterObject(CharacterObject src, CharacterObject clone)
        {
            // origin
            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(clone, origin);

            // Hero block — same logic as the original (usually false for troops)
            if (clone.IsHero)
            {
                // var staticProps = src.IsHero
                //     ? src.HeroObject.StaticBodyProperties
                //     : src.GetBodyPropertiesMin().StaticProperties;
                // clone.HeroObject.StaticBodyProperties = staticProps;
            }

            // Copy the fields the engine copies
            F_occupation.SetValue(clone, F_occupation.GetValue(src));
            F_persona.SetValue(clone, F_persona.GetValue(src));

            var traitsSrc = (CharacterTraits)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(clone, new CharacterTraits(traitsSrc));

            F_civilianEquipmentTemplate.SetValue(clone, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(clone, F_battleEquipmentTemplate.GetValue(src));

            // Fill the rest
            M_fillFrom.Invoke(clone, [src]);

            return clone;
        }
    }
}