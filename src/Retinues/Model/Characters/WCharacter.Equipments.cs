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
        //                     Serialization                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Snapshot of the entire equipment roster.
        // Stores a list of serialized MEquipment blobs. Each MEquipment uses its own
        // MBase.Serialize / Deserialize, so adding new attributes to MEquipment
        // automatically gets persisted with no changes here.
        MAttribute<string> EquipmentsSerializedAttribute =>
            Attribute(
                getter: _ => SerializeEquipments(),
                setter: (_, data) => ApplySerializedEquipments(data),
                priority: AttributePriority.Low
            );

        public void TouchEquipments() => EquipmentsSerializedAttribute.Touch();

        private string SerializeEquipments()
        {
            var roster = EquipmentRoster;
            var equipments = roster.Equipments;

            if (equipments == null || equipments.Count == 0)
                return string.Empty;

            var blobs = new List<string>(equipments.Count);
            foreach (var me in equipments)
            {
                if (me == null)
                {
                    blobs.Add(string.Empty);
                    continue;
                }
                me.MarkAllAttributesDirty();

                var blob = me.Serialize();
                blobs.Add(blob ?? string.Empty);
            }

            var result = Serialization.Serialize(blobs).Compact;
            Log.Info(
                $"WCharacter.SerializeEquipments: serialized {equipments.Count} equipments for '{StringId}'."
            );
            return result;
        }

        private void ApplySerializedEquipments(string data)
        {
            Log.Info(
                $"WCharacter.ApplySerializedEquipments: deserializing equipments for '{StringId}': {data}"
            );

            if (string.IsNullOrEmpty(data))
                return;

            var blobs = Serialization.Deserialize<List<string>>(data);
            if (blobs == null || blobs.Count == 0)
                return;

            var roster = EquipmentRoster;
            var list = new List<MEquipment>();

            foreach (var blob in blobs)
            {
                if (string.IsNullOrEmpty(blob))
                    continue;

                var me = MEquipment.Create(this);
                me.Deserialize(blob);
                list.Add(me);
            }

            if (list.Count == 0)
            {
                roster.Reset();
                return;
            }

            roster.Equipments = list;
        }
    }
}
