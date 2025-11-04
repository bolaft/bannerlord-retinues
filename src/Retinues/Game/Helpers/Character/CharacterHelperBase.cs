using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
#if BL13
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
#endif

namespace Retinues.Game.Helpers.Character
{
    /// <summary>
    /// Base class for custom/vanilla character helpers.
    /// Provides deep copy logic and helpers for copying and cloning character objects and equipment.
    /// </summary>
    public abstract class CharacterHelperBase
    {
        /// <summary>
        /// Gets a CharacterObject by vanilla troop ID.
        /// </summary>
        public CharacterObject GetCharacterObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(id);
        }

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
#if BL13
            if (src == null || tgt == null)
                return tgt;

            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            // Fields the engine copies
            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (PropertyOwner<TraitObject>)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(tgt, new PropertyOwner<TraitObject>(traitsSrc));

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            // Fill remaining data via CharacterObject.FillFrom
            M_fillFrom.Invoke(tgt, [src]);

            // Fresh roster with deep-cloned Equipment
            InstallFreshRosterFromSourceBattleSets(src, tgt);

            return tgt;
#else
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
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        /// <summary>
        /// Installs a fresh equipment roster on tgt, deep-cloned from src's battle sets.
        /// </summary>
        protected void InstallFreshRosterFromSourceBattleSets(
            CharacterObject src,
            CharacterObject tgt
        )
        {
            try
            {
                var srcEquipments = src.BattleEquipments?.ToList() ?? [];
                if (srcEquipments.Count == 0)
                    srcEquipments.Add(new Equipment(Equipment.EquipmentType.Battle));

                var cloned = new List<Equipment>(srcEquipments.Count);
                foreach (var e in srcEquipments)
                {
                    var code = e?.CalculateEquipmentCode();
                    var ne =
                        (code != null)
                            ? Equipment.CreateFromEquipmentCode(code)
                            : new Equipment(Equipment.EquipmentType.Battle);
                    try
                    {
                        AccessTools
                            .Field(typeof(Equipment), "_equipmentType")
                            ?.SetValue(ne, Equipment.EquipmentType.Battle);
                    }
                    catch { }
                    cloned.Add(ne);
                }

                var newRoster = (MBEquipmentRoster)
                    Activator.CreateInstance(typeof(MBEquipmentRoster), nonPublic: true);

                if (F_roster_equipments != null)
                    F_roster_equipments.SetValue(newRoster, new MBList<Equipment>(cloned));
                else
                    AccessTools
                        .Property(typeof(MBEquipmentRoster), "AllEquipments")
                        ?.SetValue(newRoster, new MBReadOnlyList<Equipment>(cloned), null);

                if (F_roster_default != null)
                    F_roster_default.SetValue(newRoster, cloned[0]);

                F_equipmentRoster.SetValue(tgt, newRoster);
            }
            catch
            {
                try
                {
                    F_equipmentRoster.SetValue(
                        tgt,
                        (MBEquipmentRoster)
                            Activator.CreateInstance(typeof(MBEquipmentRoster), nonPublic: true)
                    );
                }
                catch { }
            }
        }
#endif
    }
}
