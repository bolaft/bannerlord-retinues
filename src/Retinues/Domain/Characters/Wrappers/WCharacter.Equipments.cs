using System.Collections.Generic;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Access                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _initializingEquipmentRoster;

        private MEquipmentRoster _equipmentRosterCache;

        public MEquipmentRoster EquipmentRoster
        {
            get
            {
                if (_equipmentRosterCache != null)
                    return _equipmentRosterCache;

                var roster = Reflection.GetFieldValue<MBEquipmentRoster>(Base, "_equipmentRoster");

                if (roster == null)
                {
                    roster = new MBEquipmentRoster();
                    Reflection.SetFieldValue(Base, "_equipmentRoster", roster);
                }

                _equipmentRosterCache = new MEquipmentRoster(roster, this);

                if (_equipmentRosterCache.Equipments.Count == 0)
                    _equipmentRosterCache.Reset();

                EnsureFirstBattleHookAndFormation(_equipmentRosterCache);

                return _equipmentRosterCache;
            }
        }

        private void EnsureFirstBattleHookAndFormation(MEquipmentRoster mroster)
        {
            if (_initializingEquipmentRoster)
                return;

            _initializingEquipmentRoster = true;

            try
            {
                HookFirstBattleEquipment(mroster.Equipments);

                // Refresh formation without calling Equipments again.
                if (FormationClassOverride != FormationClass.Unset)
                {
                    ApplyFormation(FormationClassHelper.FromFormationClass(FormationClassOverride));
                    return;
                }

                if (_hookedFirstBattleEquipment != null)
                    ApplyFormation(_hookedFirstBattleEquipment.FormationInfo);
            }
            finally
            {
                _initializingEquipmentRoster = false;
            }
        }

        public List<MEquipment> Equipments => EquipmentRoster.Equipments;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   First Battle Hook                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private MEquipment _hookedFirstBattleEquipment;

        /// <summary>
        /// Gets the first battle equipment in the roster.
        /// </summary>
        private static MEquipment GetFirstBattleEquipment(List<MEquipment> equipments)
        {
            if (equipments == null)
                return null;

            foreach (var eq in equipments)
            {
                if (eq == null)
                    continue;

                if (eq.IsCivilian)
                    continue;

                return eq;
            }

            return null;
        }

        /// <summary>
        /// Hooks into the first battle equipment to listen for item changes.
        /// </summary>
        private void HookFirstBattleEquipment(List<MEquipment> equipments)
        {
            var eq = GetFirstBattleEquipment(equipments);
            if (ReferenceEquals(eq, _hookedFirstBattleEquipment))
                return;

            if (_hookedFirstBattleEquipment != null)
                _hookedFirstBattleEquipment.ItemsChanged -= OnFirstBattleEquipmentItemsChanged;

            _hookedFirstBattleEquipment = eq;

            if (_hookedFirstBattleEquipment != null)
                _hookedFirstBattleEquipment.ItemsChanged += OnFirstBattleEquipmentItemsChanged;
        }

        /// <summary>
        /// Called when items in the first battle equipment change.
        /// </summary>
        private void OnFirstBattleEquipmentItemsChanged(MEquipment _)
        {
            RefreshFormationFromFirstBattleEquipment();
        }

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
