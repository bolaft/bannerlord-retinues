using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

                return _equipmentRosterCache;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Convenience                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<MEquipment> Equipments => EquipmentRoster.Equipments;

        public MEquipment FirstBattleEquipment => GetFirstEquipment(civilian: false);
        public MEquipment FirstCivilianEquipment => GetFirstEquipment(civilian: true);

        /// <summary>
        /// Gets the first equipment matching the civilian flag.
        /// </summary>
        public MEquipment GetFirstEquipment(bool civilian)
        {
            foreach (var equipment in Equipments)
            {
                if (equipment?.IsCivilian == civilian)
                    return equipment;
            }

            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Serialization                     //
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

        private UpgradeItemRequirementKey _lastFirstBattleMountKey;

        /// <summary>
        /// Called when the equipment roster changes.
        /// </summary>
        public void OnEquipmentChange()
        {
            // Touch the equipment serialization attribute to ensure it gets saved.
            EquipmentsSerializedAttribute.Touch();

            // Update formation class in case the first battle equipment changed.
            UpdateFormationClass();

            // Refresh item counts cache.
            EquipmentRoster.InvalidateItemCountsCache();

            // Invalidate conversion sources cache, as equipment changes may affect eligibility.
            ConversionCache.Clear();

            // Update upgrade item requirements only when the first battle mount changed.
            var mountKey = UpgradeRequirementHelper.GetBestBattleMountKey(this);
            if (mountKey != _lastFirstBattleMountKey)
            {
                _lastFirstBattleMountKey = mountKey;
                UpdateItemRequirements(updateTargets: true);
            }
        }

        /// <summary>
        /// Serializes the equipment roster.
        /// </summary>
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

        /// <summary>
        /// Applies the serialized equipment roster.
        /// </summary>
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
