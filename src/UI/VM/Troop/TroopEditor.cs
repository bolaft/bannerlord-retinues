using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.UI.Helpers;
using CustomClanTroops.Logic;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM.Troop
{
    public sealed class TroopEditorVM(ClanScreen screen) : BaseEditor<TroopEditorVM>(screen), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public string Name => Troop?.Name;

        [DataSourceProperty]
        public string Gender => Troop != null && Troop.IsFemale ? "Female" : "Male";

        [DataSourceProperty]
        public int SkillTotal => Troop != null ? Rules.SkillTotalByTier(Troop.Tier) : 0;

        [DataSourceProperty]
        public int SkillPointsUsed => Troop?.Skills.Values.Sum() ?? 0;

        [DataSourceProperty]
        public bool IsMaxTier => Troop?.IsMaxTier ?? false;

        [DataSourceProperty]
        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets
        {
            get
            {
                var upgrades = new MBBindingList<TroopUpgradeTargetVM>();

                if (Troop != null)
                    foreach (var target in Troop.UpgradeTargets)
                        upgrades.Add(new TroopUpgradeTargetVM(target));

                return upgrades;
            }
        }

        [DataSourceProperty]
        public bool CanUpgrade => Troop != null && Rules.CanUpgradeTroop(Troop);

        [DataSourceProperty]
        public bool CanRemove
        {
            get
            {
                if (Screen.IsEquipmentMode)
                    return false; // Only show in default mode
                if (Troop?.Parent is null)
                    return false; // Cannot remove root troops
                if (Troop?.UpgradeTargets.Count() > 0)
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
            var oldName = Troop.Name;

            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: "Rename Troop", text: "Enter a new name:",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: (string newName) =>
                {
                    if (string.IsNullOrWhiteSpace(newName)) return;

                    Troop.Name = newName.Trim();

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
            Troop.IsFemale = !Troop.IsFemale;

            // UI updates
            OnPropertyChanged(nameof(Gender));
            Screen.Refresh();
            Screen.TroopList.SelectedRow.Refresh();
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
                    var upgrade = Troop.Clone(
                        clan: Troop.Clan,
                        parent: Troop,
                        keepUpgrades: false,
                        keepEquipment: false,
                        keepSkills: true
                    );

                    // Set name and level
                    upgrade.Name = name.Trim();
                    upgrade.Level = Troop.Level + 5;

                    // Add as an upgrade target
                    Troop.AddUpgradeTarget(upgrade);

                    // Add it the the clan's troop list
                    if (upgrade.IsElite)
                        Troop.Clan.EliteTroops.Add(upgrade);
                    else
                        Troop.Clan.BasicTroops.Add(upgrade);

                    // Update the troop list
                    Screen.TroopList.Refresh();

                    // Select the new troop
                    Screen.TroopList.Select(upgrade);
                },
                negativeAction: () => { },
                defaultInputText: Troop.Name
            ));
        }

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Remove Troop",
                text: $"Are you sure you want to permanently remove {Troop.Name}?\n\nTheir equipment will be stocked for later use.",
                isAffirmativeOptionShown: true, isNegativeOptionShown: true,
                affirmativeText: "Confirm", negativeText: "Cancel",
                affirmativeAction: () =>
                {
                    // Stock the troop's equipment
                    foreach (var item in Troop.Equipment.Items)
                        item.Stock();

                    // Remove the troop
                    Troop.Remove();

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
            Log.Debug("Refreshing Equipment Editor.");

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SkillTotal));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(IsMaxTier));
            OnPropertyChanged(nameof(UpgradeTargets));

            OnPropertyChanged(nameof(CanUpgrade));
            OnPropertyChanged(nameof(CanRemove));

            OnPropertyChanged(nameof(RemoveButtonHint));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private string CantRemoveTroopExplanation =>
              Troop?.Parent is null ? "Root troops cannot be removed."
            : Troop?.UpgradeTargets.Count() > 0 ? "Troops that have upgrade targets cannot be removed."
            : string.Empty;
    }
}