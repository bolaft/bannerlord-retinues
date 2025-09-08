using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Utils;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopEditorVM(EditorScreenVM screen) : BaseEditor<TroopEditorVM>(screen), IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly MBBindingList<TroopSkillVM> _skillsRow1 = [];
        private readonly MBBindingList<TroopSkillVM> _skillsRow2 = [];

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => SelectedTroop?.Name;

        [DataSourceProperty]
        public string Gender => SelectedTroop != null && SelectedTroop.IsFemale ? "Female" : "Male";

        [DataSourceProperty]
        public int SkillTotal => SelectedTroop != null ? TroopRules.SkillTotalByTier(SelectedTroop.Tier) : 0;

        [DataSourceProperty]
        public int SkillPointsUsed => SelectedTroop?.Skills.Values.Sum() ?? 0;

        [DataSourceProperty]
        public bool IsMaxTier => SelectedTroop?.IsMaxTier ?? false;

        [DataSourceProperty]
        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets
        {
            get
            {
                var upgrades = new MBBindingList<TroopUpgradeTargetVM>();

                if (SelectedTroop != null)
                    foreach (var target in SelectedTroop.UpgradeTargets)
                        upgrades.Add(new TroopUpgradeTargetVM(target));

                return upgrades;
            }
        }

        [DataSourceProperty] public MBBindingList<TroopSkillVM> SkillsRow1 => _skillsRow1;

        [DataSourceProperty] public MBBindingList<TroopSkillVM> SkillsRow2 => _skillsRow2;

        [DataSourceProperty]
        public bool CanUpgrade => SelectedTroop != null && TroopRules.CanUpgradeTroop(SelectedTroop);

        [DataSourceProperty]
        public bool CanRemove
        {
            get
            {
                if (Screen.IsEquipmentMode)
                    return false; // Only show in default mode
                if (SelectedTroop?.Parent is null)
                    return false; // Cannot remove root troops
                if (SelectedTroop?.UpgradeTargets.Count() > 0)
                    return false; // Cannot remove troops that are upgrade targets

                return true;
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (CanRemove)
                    return null; // No hint if can remove

                return Helpers.Tooltip.MakeTooltip(null, CantRemoveTroopExplanation);
            }
        }

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteRename()
        {
            var oldName = SelectedTroop.Name;

            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: "Rename Troop", text: "Enter a new name:",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: newName =>
                {
                    if (string.IsNullOrWhiteSpace(newName)) return;

                    TroopManager.Rename(SelectedTroop, newName);

                    // UI updates
                    OnPropertyChanged(nameof(Name));
                    Screen.TroopList.SelectedRow.Refresh();
                },
                negativeAction: () => { },
                defaultInputText: oldName
            ));
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            TroopManager.ChangeGender(SelectedTroop);

            // UI updates
            OnPropertyChanged(nameof(Gender));
            Screen.TroopList.SelectedRow.Refresh();
            Screen.Refresh();
        }

        [DataSourceMethod]
        public void ExecuteAddUpgradeTarget()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: "Add Upgrade", text: "Enter the name of the new troop:",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: name =>
                {
                    if (string.IsNullOrWhiteSpace(name)) return;

                    var target = TroopManager.AddUpgradeTarget(SelectedTroop, name);

                    // Update the troop list
                    Screen.TroopList.Refresh();

                    // Select the new troop
                    Screen.TroopList.Select(target);
                },
                negativeAction: () => { },
                defaultInputText: SelectedTroop.Name
            ));
        }

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Remove Troop",
                text: $"Are you sure you want to permanently remove {SelectedTroop.Name}?\n\nTheir equipment will be stocked for later use.",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: () =>
                {
                    TroopManager.Remove(SelectedTroop);

                    // Update the troop list
                    Screen.TroopList.Refresh();
                },
                negativeAction: () => { }
            ));
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            RebuildSkillRows();

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SkillTotal));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(IsMaxTier));
            OnPropertyChanged(nameof(UpgradeTargets));

            OnPropertyChanged(nameof(CanUpgrade));
            OnPropertyChanged(nameof(CanRemove));

            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));

            OnPropertyChanged(nameof(RemoveButtonHint));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private string CantRemoveTroopExplanation =>
              SelectedTroop?.Parent is null ? "Root troops cannot be removed."
            : SelectedTroop?.UpgradeTargets.Count() > 0 ? "Troops that have upgrade targets cannot be removed."
            : string.Empty;

        private void RebuildSkillRows()
        {
            Log.Debug("Rebuilding skill rows.");

            _skillsRow1.Clear();
            _skillsRow2.Clear();

            if (SelectedTroop == null) return;

            foreach (var s in SelectedTroop.Skills.Take(4))
                _skillsRow1.Add(new TroopSkillVM(s.Key, SelectedTroop, this));

            foreach (var s in SelectedTroop.Skills.Skip(4).Take(4))
                _skillsRow2.Add(new TroopSkillVM(s.Key, SelectedTroop, this));
        }
    }
}
