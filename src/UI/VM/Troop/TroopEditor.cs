using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopEditorVM(ClanScreen screen) : BaseEditor<TroopEditorVM>(screen), IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly MBBindingList<TroopSkillVM> _skillsRow1 = new();
        private readonly MBBindingList<TroopSkillVM> _skillsRow2 = new();

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => SelectedTroop?.Name;

        [DataSourceProperty]
        public string Gender => SelectedTroop != null && SelectedTroop.IsFemale ? "Female" : "Male";

        [DataSourceProperty]
        public int SkillTotal => SelectedTroop != null ? Rules.SkillTotalByTier(SelectedTroop.Tier) : 0;

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
        public bool CanUpgrade => SelectedTroop != null && Rules.CanUpgradeTroop(SelectedTroop);

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
                affirmativeAction: (string newName) =>
                {
                    if (string.IsNullOrWhiteSpace(newName)) return;

                    SelectedTroop.Name = newName.Trim();

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
            SelectedTroop.IsFemale = !SelectedTroop.IsFemale;

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
                affirmativeAction: (string name) =>
                {
                    if (string.IsNullOrWhiteSpace(name)) return;

                    // Create the upgrade troop by cloning
                    var upgrade = SelectedTroop.Clone(
                        clan: SelectedTroop.Clan,
                        parent: SelectedTroop,
                        keepUpgrades: false,
                        keepEquipment: false,
                        keepSkills: true
                    );

                    // Set name and level
                    upgrade.Name = name.Trim();
                    upgrade.Level = SelectedTroop.Level + 5;

                    // Add as an upgrade target
                    SelectedTroop.AddUpgradeTarget(upgrade);

                    // Add it the the clan's troop list
                    if (upgrade.IsElite)
                        SelectedTroop.Clan.EliteTroops.Add(upgrade);
                    else
                        SelectedTroop.Clan.BasicTroops.Add(upgrade);

                    // Update the troop list
                    Screen.TroopList.Refresh();

                    // Select the new troop
                    Screen.TroopList.Select(upgrade);
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
                    // Stock the troop's equipment
                    foreach (var item in SelectedTroop.Equipment.Items)
                        item.Stock();

                    // Remove the troop
                    SelectedTroop.Remove();

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
