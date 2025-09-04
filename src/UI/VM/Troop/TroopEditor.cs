using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
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
        public bool CanUpgrade => Troop != null && Rules.CanUpgradeTroop(Troop);

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

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing Equipment Editor.");

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
        }
    }
}