using System.Collections.Generic;
using Retinues.Model.Equipments;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBEquipmentRoster> EquipmentRosterAttribute =>
            Attribute<MBEquipmentRoster>("_equipmentRoster");

        public WEquipmentRoster EquipmentRoster
        {
            get => WEquipmentRoster.Get(EquipmentRosterAttribute.Get());
            set => EquipmentRosterAttribute.Set(value?.Base);
        }

        void EnsureEquipmentRoster()
        {
            if (EquipmentRosterAttribute.Get() == null)
                EquipmentRosterAttribute.Set(new MBEquipmentRoster());
        }

        MAttribute<string> _equipmentsDataAttribute;
        string _equipmentsDataCache = string.Empty;

        public MAttribute<string> EquipmentsDataAttribute =>
            _equipmentsDataAttribute ??= new MAttribute<string>(
                baseInstance: Base,
                getter: _ => _equipmentsDataCache,
                setter: (_, value) =>
                {
                    _equipmentsDataCache = value ?? string.Empty;
                    ApplyEquipmentsData(_equipmentsDataCache);
                },
                targetName: "equipments",
                persistent: true
            );

        public string EquipmentsData
        {
            get => EquipmentsDataAttribute.Get();
            set => EquipmentsDataAttribute.Set(value);
        }

        void ApplyEquipmentsData(string data)
        {
            EnsureEquipmentRoster();

            var roster = EquipmentRoster;
            if (roster == null)
                return;

            var list = MEquipment.DeserializeMany(data);

            var raw = new MBList<Equipment>();
            foreach (var me in list)
            {
                if (me?.Base == null)
                    continue;

                raw.Add(me.Base);
            }

            roster.RawEquipments = raw;
        }

        void RebuildEquipmentsDataFromRoster(bool touch)
        {
            EnsureEquipmentRoster();

            var roster = EquipmentRoster;
            var raw = roster?.RawEquipments;

            if (raw == null || raw.Count == 0)
            {
                _equipmentsDataCache = string.Empty;
            }
            else
            {
                var list = new List<MEquipment>(raw.Count);
                for (int i = 0; i < raw.Count; i++)
                {
                    var eq = raw[i];
                    if (eq == null)
                        continue;

                    list.Add(new MEquipment(eq));
                }

                _equipmentsDataCache = MEquipment.SerializeMany(list);
            }

            if (touch)
                EquipmentsDataAttribute.Touch();
        }

        public List<MEquipment> Equipments
        {
            get
            {
                EnsureEquipmentRoster();

                var roster = EquipmentRoster;
                var raw = roster?.RawEquipments;

                if (raw == null || raw.Count == 0)
                    return [];

                var result = new List<MEquipment>(raw.Count);
                for (int i = 0; i < raw.Count; i++)
                {
                    var eq = raw[i];
                    if (eq == null)
                        continue;

                    result.Add(new MEquipment(eq) { Owner = this });
                }

                return result;
            }
        }

        internal void OnEquipmentChanged()
        {
            RebuildEquipmentsDataFromRoster(touch: true);
        }

        public void AddEquipment(MEquipment equipment, int index = -1)
        {
            if (equipment?.Base == null)
                return;

            EnsureEquipmentRoster();

            var roster = EquipmentRoster;
            var raw = roster.RawEquipments;

            if (index < 0 || index > raw.Count)
                index = raw.Count;

            raw.Insert(index, equipment.Base);
            roster.RawEquipments = raw;

            RebuildEquipmentsDataFromRoster(touch: true);
        }

        public void RemoveEquipment(int index)
        {
            EnsureEquipmentRoster();

            var roster = EquipmentRoster;
            var raw = roster.RawEquipments;

            if (index < 0 || index >= raw.Count)
                return;

            raw.RemoveAt(index);
            roster.RawEquipments = raw;

            RebuildEquipmentsDataFromRoster(touch: true);
        }
    }
}
