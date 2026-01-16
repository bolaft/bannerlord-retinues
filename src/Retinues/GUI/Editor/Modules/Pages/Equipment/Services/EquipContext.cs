using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
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
            !PreviewEnabled && Mode == EditorMode.Player && Settings.EquipmentCostsMoney;

        public bool LimitsEnabled =>
            Mode == EditorMode.Player
            || (Mode == EditorMode.Universal && Settings.EnforceEquipmentLimitsInUniversalMode);

        public bool WeightLimitActive => LimitsEnabled && Settings.EquipmentWeightLimit;
        public bool ValueLimitActive => LimitsEnabled && Settings.EquipmentValueLimit;
    }
}
