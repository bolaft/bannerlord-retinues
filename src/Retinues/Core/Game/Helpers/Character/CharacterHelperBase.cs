using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Core.Game.Helpers.Character
{
    /// <summary>
    /// Base class for custom/vanilla character helpers.
    /// Provides deep copy logic and helpers for copying and cloning character objects and equipment.
    /// </summary>
    public abstract class CharacterHelperBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Reflection Handles                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static readonly FieldInfo F_originCharacter = AccessTools.Field(
            typeof(CharacterObject),
            "_originCharacter"
        );
        protected static readonly FieldInfo F_occupation = AccessTools.Field(
            typeof(CharacterObject),
            "_occupation"
        );
        protected static readonly FieldInfo F_persona = AccessTools.Field(
            typeof(CharacterObject),
            "_persona"
        );
        protected static readonly FieldInfo F_characterTraits = AccessTools.Field(
            typeof(CharacterObject),
            "_characterTraits"
        );
        protected static readonly FieldInfo F_civilianEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_civilianEquipmentTemplate"
        );
        protected static readonly FieldInfo F_battleEquipmentTemplate = AccessTools.Field(
            typeof(CharacterObject),
            "_battleEquipmentTemplate"
        );
        protected static readonly FieldInfo F_equipmentRoster = AccessTools.Field(
            typeof(BasicCharacterObject),
            "_equipmentRoster"
        );
        protected static readonly FieldInfo F_roster_equipments = AccessTools.Field(
            typeof(MBEquipmentRoster),
            "_equipments"
        );
        protected static readonly FieldInfo F_roster_default = AccessTools.Field(
            typeof(MBEquipmentRoster),
            "_defaultEquipment"
        );
        protected static readonly MethodInfo M_fillFrom = AccessTools.Method(
            typeof(CharacterObject),
            "FillFrom",
            [typeof(CharacterObject)]
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Deep Copy                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deep-copies all relevant fields from src to tgt, including traits, persona, equipment, and origin.
        /// </summary>
        public virtual CharacterObject CopyInto(CharacterObject src, CharacterObject tgt)
        {
            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            // Copy the fields the engine copies
            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (CharacterTraits)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(
                tgt,
                traitsSrc != null ? new CharacterTraits(traitsSrc) : null
            );

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            // Fill the rest
            M_fillFrom.Invoke(tgt, [src]);

            return tgt;
        }
    }
}
