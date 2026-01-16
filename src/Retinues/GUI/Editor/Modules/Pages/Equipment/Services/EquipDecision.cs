using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Services
{
    /// <summary>
    /// Result of validating a single slot change (equip/unequip).
    /// </summary>
    public readonly struct EquipDecision
    {
        public bool Allowed { get; }
        public EquipSkipReason Reason { get; }
        public TextObject Tooltip { get; }

        private EquipDecision(bool allowed, EquipSkipReason reason, TextObject tooltip)
        {
            Allowed = allowed;
            Reason = reason;
            Tooltip = tooltip;
        }

        public static EquipDecision Ok() => new(true, EquipSkipReason.None, null);

        public static EquipDecision Skip(EquipSkipReason reason, TextObject tooltip) =>
            new(false, reason, tooltip);
    }
}
