using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Equipment.Controllers
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

        /// <summary>
        /// Ensures the preview clone is still valid for the current selection and disables preview if it is not.
        /// </summary>
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

        /// <summary>
        /// Enables or disables preview mode for equipment edits.
        /// </summary>
        public static ControllerAction<bool> SetPreviewMode { get; } =
            Action<bool>("SetPreviewMode")
                .DefaultTooltip(enable =>
                    enable
                        ? L.T("preview_mode_enable_tooltip", "Enable preview mode.")
                        : L.T("preview_mode_disable_tooltip", "Disable preview mode.")
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

        /// <summary>
        /// Enable preview mode by cloning the currently selected equipment for temporary edits.
        /// </summary>
        public static void EnablePreview()
        {
            if (_preview != null)
                return;

            var src = State.Equipment?.Base;
            if (src == null)
                return;

            _source = src;
            _preview = new TaleWorlds.Core.Equipment(src);

            Log.Debug("Preview mode enabled.");

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Preview);
                EventManager.Fire(UIEvent.Item);
                EventManager.Fire(UIEvent.Appearance);
            });
        }

        /// <summary>
        /// Disable preview mode and discard the preview clone, restoring normal visuals.
        /// </summary>
        public static void DisablePreview()
        {
            if (_preview == null)
                return;

            _source = null;
            _preview = null;

            Log.Debug("Preview mode disabled.");

            EventManager.FireBatch(() =>
            {
                EventManager.Fire(UIEvent.Preview);
                EventManager.Fire(UIEvent.Item);
                EventManager.Fire(UIEvent.Appearance);
            });
        }

        /// <summary>
        /// Equipment to apply on the 3D model.
        /// Returns preview equipment when enabled and valid, otherwise the real selected equipment.
        /// </summary>
        public static TaleWorlds.Core.Equipment GetEquipmentForModel()
        {
            // Preview mode: already a clone, safe to return.
            if (EnsureValid())
                return _preview;

            var real = State.Equipment?.Base;
            if (real == null)
                return null;

            // Important: NEVER return the real equipment instance here.
            // Callers are allowed to mutate this for visuals, and that must not persist.
            var model = new TaleWorlds.Core.Equipment(real);

            // Also reflect staged/planned items on the 3D model.
            // State.Equipment.Get(slot) returns staged when staging is active, otherwise real.
            var me = State.Equipment;
            if (me != null)
            {
                int slots = (int)EquipmentIndex.NumEquipmentSetSlots;

                for (int i = 0; i < slots; i++)
                {
                    var slot = (EquipmentIndex)i;
                    var item = me.Get(slot);

                    model[slot] =
                        item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
                }
            }

            return model;
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
        /// Checks whether the given harness is compatible with the given horse.
        /// </summary>
        private static bool IsHarnessCompatible(WItem horse, WItem harness)
        {
            if (horse == null || harness == null)
                return true;

            return horse.IsCompatibleWith(harness);
        }

        /// <summary>
        /// Sets a slot in the preview equipment. No-op when preview is disabled.
        /// This does not touch the real MEquipment or any roster or economy.
        /// </summary>
        public static void SetItem(EquipmentIndex slot, WItem item)
        {
            if (!EnsureValid())
                return;

            // Read current preview state (important: we haven't written yet).
            var currentHorse = GetItem(EquipmentIndex.Horse);
            var currentHarness = GetItem(EquipmentIndex.HorseHarness);

            if (slot == EquipmentIndex.HorseHarness)
            {
                // Reject incompatible harness.
                if (!IsHarnessCompatible(currentHorse, item))
                    return;
            }

            _preview[slot] =
                item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);

            if (slot == EquipmentIndex.Horse)
            {
                // After changing horse, clear harness if it no longer fits.
                if (!IsHarnessCompatible(item, currentHarness))
                    _preview[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
            }

            EventManager.Fire(UIEvent.Item);
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

            EventManager.Fire(UIEvent.Item);
        }
    }
}
