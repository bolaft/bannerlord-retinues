using System.Collections.Generic;
using System.Linq;
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

        public MEquipmentRoster EquipmentRoster =>
            new(Reflection.GetFieldValue<MBEquipmentRoster>(Base, "_equipmentRoster"), this);
        public List<MEquipment> Equipments => EquipmentRoster.Equipments;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Mutations                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void AddEquipment(MEquipment equipment) => EquipmentRoster.Add(equipment);

        public void RemoveEquipment(MEquipment equipment) => EquipmentRoster.Remove(equipment);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Persistence                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<string> _equipmentsSerializedAttribute;
        bool _equipmentsDirtyDeferred;
        bool _suppressEquipmentsDirty;

        internal void MarkEquipmentsDirty()
        {
            if (_suppressEquipmentsDirty)
                return;

            // Do NOT force creation of the attribute during gameplay mutations.
            if (_equipmentsSerializedAttribute != null)
                _equipmentsSerializedAttribute.Touch();
            else
                _equipmentsDirtyDeferred = true;
        }

        /// <summary>
        /// Serialized representation of all equipment sets for this character.
        /// </summary>
        public MAttribute<string> EquipmentsSerializedAttribute
        {
            get
            {
                if (_equipmentsSerializedAttribute == null)
                {
                    var attr = new MAttribute<string>(
                        baseInstance: Base,
                        getter: _ => SerializeEquipments(),
                        setter: (_, value) => ApplySerializedEquipments(value),
                        targetName: "equipments_serialized"
                    );

                    _equipmentsSerializedAttribute = attr;

                    if (_equipmentsDirtyDeferred)
                    {
                        _equipmentsDirtyDeferred = false;
                        _equipmentsSerializedAttribute.Touch();
                    }
                }

                return _equipmentsSerializedAttribute;
            }
        }

        private void ApplySerializedEquipments(string str)
        {
            Log.Info($"Deserializing equipments for '{StringId}': {str}");
            var data = Serialization.DeserializeList(str);

            _suppressEquipmentsDirty = true;
            try
            {
                var list = data.Select(s => MEquipment.Deserialize(s, this)).ToList();
                EquipmentRoster.Equipments = list;
            }
            finally
            {
                _suppressEquipmentsDirty = false;
            }
        }

        /// <summary>
        /// Serializes all equipment sets for this character.
        /// </summary>
        private string SerializeEquipments()
        {
            List<string> equipments = [.. Equipments.Select(me => me.Serialize())];
            return Serialization.SerializeList(equipments);
        }
    }
}
