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
        MAttribute<List<string>> EquipmentsSerializedAttribute =>
            Attribute(
                getter: _ => SerializeEquipments(),
                setter: (_, data) => ApplySerializedEquipments(data),
                priority: AttributePriority.Low
            );

        public void TouchEquipments() => EquipmentsSerializedAttribute.Touch();

        private List<string> SerializeEquipments()
        {
            var roster = EquipmentRoster;
            var equipments = roster.Equipments;

            if (equipments == null || equipments.Count == 0)
                return null;

            var blobs = new List<string>(equipments.Count);
            foreach (var me in equipments)
            {
                if (me == null)
                {
                    blobs.Add(string.Empty);
                    continue;
                }

                // Ensure the equipment snapshot is complete (even if nothing else on the troop was touched).
                me.MarkAllAttributesDirty();

                var blob = me.Serialize();
                blobs.Add(blob ?? string.Empty);
            }

            Log.Info(
                $"WCharacter.SerializeEquipments: serialized {equipments.Count} equipments for '{StringId}'."
            );

            return blobs;
        }

        private void ApplySerializedEquipments(List<string> blobs)
        {
            if (blobs == null || blobs.Count == 0)
            {
                // No saved data: keep vanilla roster (or whatever is already on Base).
                return;
            }

            var roster = EquipmentRoster;
            var list = new List<MEquipment>(blobs.Count);

            foreach (var blob in blobs)
            {
                if (string.IsNullOrWhiteSpace(blob))
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
