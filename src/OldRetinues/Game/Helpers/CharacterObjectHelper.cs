using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
#if BL13
using TaleWorlds.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
#endif

namespace OldRetinues.Game.Helpers
{
    public class CharacterObjectHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Deep Copy                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Deep-copies key data from src to tgt and calls FillFrom.
        /// BL13 path also installs a fresh battle equipment roster cloned from src.
        /// </summary>
        public static CharacterObject CopyInto(CharacterObject src, CharacterObject tgt)
        {
#if BL13
            if (src == null || tgt == null)
                return tgt;

            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (PropertyOwner<TraitObject>)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(tgt, new PropertyOwner<TraitObject>(traitsSrc));

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            M_fillFrom.Invoke(tgt, new object[] { src });

            InstallFreshRosterFromSourceBattleSets(src, tgt);
            return tgt;
#else
            if (src == null || tgt == null)
                return tgt;

            var origin = (CharacterObject)F_originCharacter.GetValue(src) ?? src;
            F_originCharacter.SetValue(tgt, origin);

            F_occupation.SetValue(tgt, F_occupation.GetValue(src));
            F_persona.SetValue(tgt, F_persona.GetValue(src));

            var traitsSrc = (CharacterTraits)F_characterTraits.GetValue(src);
            F_characterTraits.SetValue(
                tgt,
                traitsSrc != null ? new CharacterTraits(traitsSrc) : null
            );

            F_civilianEquipmentTemplate.SetValue(tgt, F_civilianEquipmentTemplate.GetValue(src));
            F_battleEquipmentTemplate.SetValue(tgt, F_battleEquipmentTemplate.GetValue(src));

            M_fillFrom.Invoke(tgt, [src]);
            return tgt;
#endif
        }

#if BL13

        /// <summary>
        /// Installs a fresh equipment roster on tgt, deep-cloned from src's battle sets.
        /// </summary>
        protected static void InstallFreshRosterFromSourceBattleSets(
            CharacterObject src,
            CharacterObject tgt
        )
        {
            try
            {
                var srcEquipments = src.BattleEquipments?.ToList() ?? new List<Equipment>();
                if (srcEquipments.Count == 0)
                    srcEquipments.Add(new Equipment(Equipment.EquipmentType.Battle));

                var cloned = new List<Equipment>(srcEquipments.Count);
                foreach (var e in srcEquipments)
                {
                    var code = e != null ? e.CalculateEquipmentCode() : null;
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Reflection Handles                   //
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

        protected static readonly MethodInfo M_fillFrom = AccessTools.Method(
            typeof(CharacterObject),
            "FillFrom",
            [typeof(CharacterObject)]
        );
#if BL13
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
#endif
    }
}
