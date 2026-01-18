namespace Retinues.Editor.MVC.Pages.Equipment.Services
{
    /// <summary>
    /// Reasons for skipping equipment operations.
    /// </summary>
    public enum EquipSkipReason
    {
        None = 0,
        Locked,
        Tier,
        Skill,
        Limits,
        CivilianMismatch,
        Incompatible,
        Other,
    }
}
