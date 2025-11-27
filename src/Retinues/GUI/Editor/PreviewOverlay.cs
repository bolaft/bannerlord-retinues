using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;

namespace Retinues.GUI.Editor
{
    /// <summary>
    /// Keeps track of "appearance-only" preview items per troop / set / slot.
    /// Does not touch staging, stocks or gold. Only drives the 3D model overlay.
    /// </summary>
    [SafeClass]
    internal static class PreviewOverlay
    {
        private static readonly Dictionary<
            (string TroopId, int SetIndex, EquipmentIndex Slot),
            WItem
        > _map = [];

        public static bool IsEnabled { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Toggle                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Enable()
        {
            if (IsEnabled)
                return;

            Log.Debug("[PreviewOverlay] Enabling preview mode.");
            IsEnabled = true;
            _map.Clear();

            // Refresh 3D model
            EventManager.Fire(UIEvent.Appearance);
        }

        public static void Disable()
        {
            if (!IsEnabled)
                return;

            Log.Debug("[PreviewOverlay] Disabling preview mode and clearing overlays.");
            IsEnabled = false;
            _map.Clear();

            // Refresh 3D model back to staged/real equipment
            EventManager.Fire(UIEvent.Appearance);
        }

        public static void Toggle()
        {
            if (IsEnabled)
                Disable();
            else
                Enable();
        }

        /// <summary>
        /// Clears all preview overlays (used when switching troop).
        /// </summary>
        public static void ClearAll()
        {
            if (_map.Count == 0)
                return;

            Log.Debug("[PreviewOverlay] Clearing all preview overlays.");
            _map.Clear();

            if (IsEnabled)
                EventManager.Fire(UIEvent.Appearance);
        }

        /// <summary>
        /// Clears preview overlays for a specific troop (optional).
        /// </summary>
        public static void ClearForTroop(WCharacter troop)
        {
            if (troop == null || _map.Count == 0)
                return;

            var id = troop.StringId;
            var toRemove = new List<(string TroopId, int SetIndex, EquipmentIndex Slot)>();

            foreach (var key in _map.Keys)
                if (key.TroopId == id)
                    toRemove.Add(key);

            if (toRemove.Count == 0)
                return;

            foreach (var key in toRemove)
                _map.Remove(key);

            if (IsEnabled)
                EventManager.Fire(UIEvent.Appearance);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Operations                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Sets or clears the preview item for a single slot.
        /// </summary>
        public static void SetPreview(
            WCharacter troop,
            int setIndex,
            EquipmentIndex slot,
            WItem item
        )
        {
            if (!IsEnabled || troop == null)
                return;

            var key = (troop.StringId, setIndex, slot);

            if (item == null)
            {
                Log.Debug(
                    $"[PreviewOverlay] Clearing preview for {troop.StringId}, set {setIndex}, slot {slot}."
                );
                _map.Remove(key);
            }
            else
            {
                Log.Debug(
                    $"[PreviewOverlay] Setting preview {item.StringId} for {troop.StringId}, set {setIndex}, slot {slot}."
                );
                _map[key] = item;
            }

            // Only the 3D model needs to refresh here.
            EventManager.Fire(UIEvent.Appearance);
        }

        /// <summary>
        /// Builds an equipment instance for the given troop/set,
        /// overlaying any previewed slots on top of the base equipment.
        /// Returns false if there is nothing to preview.
        /// </summary>
        public static bool TryBuildEquipment(
            WCharacter troop,
            int setIndex,
            Equipment baseEquipment,
            out Equipment result
        )
        {
            result = null;

            if (!IsEnabled || troop == null || baseEquipment == null)
                return false;

            var id = troop.StringId;
            var hasAny = false;
            var copy = new Equipment(baseEquipment);

            foreach (var pair in _map)
            {
                var (TroopId, SetIndex, Slot) = pair.Key;
                if (TroopId != id || SetIndex != setIndex)
                    continue;

                var item = pair.Value;
                if (item?.Base == null)
                    continue;

                copy[Slot] = new EquipmentElement(item.Base);
                hasAny = true;
            }

            if (!hasAny)
                return false;

            result = copy;
            return true;
        }
    }
}
