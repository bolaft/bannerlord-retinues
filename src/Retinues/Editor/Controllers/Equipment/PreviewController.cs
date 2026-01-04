using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Equipment
{
    /// <summary>
    /// Temporary equipment preview.
    /// When enabled, equip and unequip operations should write into a cloned Equipment instance
    /// that is only used for the 3D model. Disabling preview discards the clone and restores the
    /// original visuals.
    /// </summary>
    public class PreviewController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         State                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static TaleWorlds.Core.Equipment _source;
        private static TaleWorlds.Core.Equipment _preview;

        public static bool Enabled => _preview != null;

        private static bool EnsureValid()
        {
            if (_preview == null)
                return false;

            var current = State.Equipment?.Base;

            // Selection changed or equipment disappeared: exit preview to avoid visual desync.
            if (current == null || !ReferenceEquals(current, _source))
            {
                DisablePreview();
                return false;
            }

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Action                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SetPreviewMode { get; } =
            Action<bool>("SetPreviewMode")
                .AddCondition(
                    enable => !enable || State.Equipment != null,
                    L.T("cant_preview_reason_no_equipment", "No equipment set.")
                )
                .ExecuteWith(enable =>
                {
                    if (enable)
                        EnablePreview();
                    else
                        DisablePreview();
                })
                .Fire(UIEvent.Appearance);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void EnablePreview()
        {
            if (_preview != null)
                return;

            var src = State.Equipment?.Base;
            if (src == null)
                return;

            _source = src;
            _preview = new TaleWorlds.Core.Equipment(src);

            Log.Info("Preview mode enabled.");

            EventManager.Fire(UIEvent.Preview);
        }

        public static void DisablePreview()
        {
            if (_preview == null)
                return;

            _source = null;
            _preview = null;

            Log.Info("Preview mode disabled.");

            EventManager.Fire(UIEvent.Preview);
        }

        /// <summary>
        /// Equipment to apply on the 3D model.
        /// Returns preview equipment when enabled and valid, otherwise the real selected equipment.
        /// </summary>
        public static TaleWorlds.Core.Equipment GetEquipmentForModel()
        {
            if (EnsureValid())
                return _preview;

            return State.Equipment?.Base;
        }

        /// <summary>
        /// Gets the currently equipped item in the given slot, using preview state when enabled.
        /// </summary>
        public static WItem GetItem(EquipmentIndex slot)
        {
            if (EnsureValid())
            {
                var item = _preview[slot].Item;
                return item == null ? null : WItem.Get(item);
            }

            return State.Equipment?.Get(slot);
        }

        /// <summary>
        /// Sets a slot in the preview equipment. No-op when preview is disabled.
        /// This does not touch the real MEquipment or any roster or economy.
        /// </summary>
        public static void SetItem(EquipmentIndex slot, WItem item)
        {
            if (!EnsureValid())
                return;

            // Safety: prevent incompatible harness equip against the current preview horse.
            if (slot == EquipmentIndex.HorseHarness)
            {
                var horse = GetItem(EquipmentIndex.Horse);
                if (horse != null && item != null && !horse.IsCompatibleWith(item))
                    return;
            }

            _preview[slot] =
                item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);

            // If a new horse makes the currently equipped harness incompatible, clear the harness.
            if (slot == EquipmentIndex.Horse)
            {
                var harness = GetItem(EquipmentIndex.HorseHarness);
                if (harness != null && item != null && !item.IsCompatibleWith(harness))
                    _preview[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
            }

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Item);
                EventManager.Fire(UIEvent.Appearance);
            });
        }

        /// <summary>
        /// Clears a slot in the preview equipment. No-op when preview is disabled.
        /// </summary>
        public static void ClearItem(EquipmentIndex slot)
        {
            if (!EnsureValid())
                return;

            _preview[slot] = EquipmentElement.Invalid;

            if (slot == EquipmentIndex.Horse)
            {
                // No behavior change: unequipping a horse also clears harness.
                _preview[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
            }

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Item);
                EventManager.Fire(UIEvent.Appearance);
            });
        }
    }
}
