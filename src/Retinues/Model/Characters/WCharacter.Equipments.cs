using System.Collections.Generic;
using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Access                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MEquipmentRoster EquipmentRoster
        {
            get
            {
                var roster = Reflection.GetFieldValue<MBEquipmentRoster>(Base, "_equipmentRoster");

                if (roster == null)
                {
                    roster = new MBEquipmentRoster();
                    Reflection.SetFieldValue(Base, "_equipmentRoster", roster);
                }

                var mroster = new MEquipmentRoster(roster, this);

                if (mroster.Equipments.Count == 0)
                    mroster.Reset();

                return mroster;
            }
        }

        public List<MEquipment> Equipments => EquipmentRoster.Equipments;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<string> EquipmentRosterSerializedAttribute =>
            Attribute(
                getter: _ => EquipmentRoster.Serialize(),
                setter: (_, s) => EquipmentRoster.Deserialize(s),
                persistent: true
            );

        internal void TouchEquipments() => EquipmentRosterSerializedAttribute.Touch();
    }
}
