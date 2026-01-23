using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Settings;

namespace Retinues.Editor.MVC.Pages.Equipment.Services
{
    /// <summary>
    /// Captures the editor state relevant to equip validation/planning.
    /// </summary>
    public sealed class EquipContext(
        EditorMode mode,
        bool previewEnabled,
        WCharacter character,
        MEquipment equipment
    )
    {
        public EditorMode Mode { get; } = mode;
        public bool PreviewEnabled { get; } = previewEnabled;

        public WCharacter Character { get; } = character;
        public MEquipment Equipment { get; } = equipment;

        public bool EconomyEnabled =>
            !PreviewEnabled && Mode == EditorMode.Player && Configuration.EquipmentCostsMoney;

        public bool LimitsEnabled =>
            Mode == EditorMode.Player
            || (
                Mode == EditorMode.Universal && Configuration.EnforceEquipmentLimitsInUniversalMode
            );

        public bool WeightLimitActive => LimitsEnabled && Configuration.EquipmentWeightLimit;
        public bool ValueLimitActive => LimitsEnabled && Configuration.EquipmentValueLimit;
    }
}
