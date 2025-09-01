using System;
using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        public CharacterWrapper Troop => _owner.SelectedTroop;

        public TroopEditorVM(ClanManagementMixinVM owner) => _owner = owner;

        [DataSourceProperty] public string Name => Troop?.Name;

        [DataSourceProperty] public string Gender => Troop?.IsFemale == true ? "Female" : "Male";

        [DataSourceProperty] public int SkillPoints => Troop?.SkillPoints ?? 0;

        [DataSourceProperty] public int SkillPointsUsed => Troop?.Skills.Values.Sum() ?? 0;

        [DataSourceProperty] public bool IsMaxTier => Troop?.Tier >= (TroopManager.IsNoble(Troop) ? 6 : 5);

        [DataSourceProperty] public bool CanAddUpgradeTarget
        {
            get
            {
                if (Troop == null) return false;
                if (IsMaxTier) return false;
                if (Troop.UpgradeTargets.Count() >= 4) return false;

                return true;
            }
        }

        [DataSourceProperty]
        public MBBindingList<UpgradeTargetVM> UpgradeTargets
        {
            get
            {
                var names = new MBBindingList<UpgradeTargetVM>();

                if (Troop == null) return names;

                foreach (var target in Troop.UpgradeTargets)
                    names.Add(new UpgradeTargetVM(new CharacterWrapper(target)));

                return names;
            }
        }

        [DataSourceProperty]
        public MBBindingList<SkillVM> SkillsRow1 => BuildSkillRow(0, 4);

        [DataSourceProperty]
        public MBBindingList<SkillVM> SkillsRow2 => BuildSkillRow(4, 4);

        private MBBindingList<SkillVM> BuildSkillRow(int skip, int take)
        {
            var skills = new MBBindingList<SkillVM>();
            if (Troop == null) return skills;
            foreach (var skill in Troop.Skills.Skip(skip).Take(take))
                skills.Add(new SkillVM(skill.Key, this));
            return skills;
        }

        [DataSourceMethod]
        public void ExecuteRename()
        {
            Log.Debug($"{nameof(ExecuteRename)} called.");

            var current = Troop.Name;

            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Rename Troop", "Enter the new name:",
                true, true,
                "Confirm", "Cancel",
                input =>
                {
                    input = (input ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(input)) return;
                    Troop.Name = input;
                    _owner.SelectedRow?.Refresh();
                    OnPropertyChanged(nameof(Name));

                    Log.Debug($"Troop renamed '{current}' → '{input}'");
                },
                null, false,
                s =>
                {
                    var ok = !string.IsNullOrWhiteSpace(s?.Trim());
                    return Tuple.Create(ok, ok ? "" : "Name cannot be empty.");
                },
                current
            ));
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            Log.Debug($"{nameof(ExecuteChangeGender)} called.");

            Troop.IsFemale = !Troop.IsFemale;

            OnPropertyChanged(nameof(Gender));
            _owner.RefreshTroopViewModel();
            _owner.SelectedRow.Refresh();
            _owner.SelectById(Troop.StringId);
        }

        [DataSourceMethod]
        public void ExecuteAddUpgradeTarget()
        {
            Log.Debug($"{nameof(ExecuteAddUpgradeTarget)} called.");

            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Add Upgrade", "Enter the name of the new troop:",
                true, true,
                "Confirm", "Cancel",
                input =>
                {
                    input = (input ?? "").Trim();
    
                    if (string.IsNullOrWhiteSpace(input)) return;

                    var newTroop = Troop.Clone();
                    newTroop.Name = input;
                    newTroop.Level = Troop.Level + 5;
                    newTroop.Parent = Troop;

                    if (TroopManager.EliteCustomTroops.Contains(Troop))
                        TroopManager.AddEliteTroop(newTroop);
                    else
                        TroopManager.AddBasicTroop(newTroop);

                    Troop.AddUpgradeTarget(newTroop);

                    _owner.TroopList.Refresh();
                    _owner.SelectById(newTroop.StringId);
                    OnPropertyChanged(nameof(UpgradeTargets));

                    Log.Debug($"Upgrade target added: '{input}'");
                },
                null, false,
                s =>
                {
                    var ok = !string.IsNullOrWhiteSpace(s?.Trim());
                    return Tuple.Create(ok, ok ? "" : "Name cannot be empty.");
                }
            ));
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SkillPoints));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));
            OnPropertyChanged(nameof(UpgradeTargets));
            OnPropertyChanged(nameof(IsMaxTier));
            OnPropertyChanged(nameof(CanAddUpgradeTarget));
        }
    }
}
