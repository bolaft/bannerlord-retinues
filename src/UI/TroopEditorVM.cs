using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.InputSystem;
using TaleWorlds.Core;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
{
    public sealed class TroopEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        private CharacterWrapper _troop => _owner?.TroopRow?.Troop;

        public TroopEditorVM(ClanManagementMixinVM owner) => _owner = owner;

        [DataSourceProperty] public string Gender => _troop.IsFemale ? "Female" : "Male";

        [DataSourceProperty] public string Name => _troop.Name;

        [DataSourceProperty]
        public bool IsElite
        {
            get
            {
                var row = _owner?.TroopRow;
                return row != null && _owner.CustomElite.Contains(row);
            }
        }

        [DataSourceProperty] public bool CanRemoveTroop
        {
            get
            {
                if (HasUpgradeTarget1) return false;
                if (_troop.GetParent() == null) return false;
                return true;
            }
        }

        [DataSourceProperty]
        public bool IsMaxTier
        {
            get
            {
                if (IsElite && _troop.Tier < 6) return false;
                if (!IsElite && _troop.Tier < 5) return false;
                return true;
            }
        }

        [DataSourceProperty] public bool CanAddUpgradeTarget
        {
            get
            {
                if (HasUpgradeTarget2) return false;
                if (!IsMaxTier) return true;
                return false;
            }
        }

        [DataSourceProperty] public bool HasUpgradeTarget1 => _troop.UpgradeTarget1 != null;

        [DataSourceProperty] public string UpgradeTarget1Id => HasUpgradeTarget1 ? _troop.UpgradeTarget1.StringId : null;

        [DataSourceProperty] public string UpgradeTarget1Name => HasUpgradeTarget1 ? _troop.UpgradeTarget1.Name : null;

        [DataSourceProperty] public bool HasUpgradeTarget2 => _troop.UpgradeTarget2 != null;

        [DataSourceProperty] public string UpgradeTarget2Id => HasUpgradeTarget2 ? _troop.UpgradeTarget2.StringId : null;

        [DataSourceProperty] public string UpgradeTarget2Name => HasUpgradeTarget2 ? _troop.UpgradeTarget2.Name : null;

        [DataSourceProperty] public int SkillPointsTotal => CharacterWrapper.TotalSkillPointsByTier[_troop.Tier];

        [DataSourceProperty] public int SkillPointsLeft
        {
            get
            {
                var points = SkillPointsTotal;
                foreach (var skill in CharacterWrapper.TroopSkills)
                {
                    points -= _troop.Skills.FirstOrDefault(s => s.skill == skill).value;
                }
                return points;
            }
        }

        [DataSourceProperty] public int SkillPointsUsed => SkillPointsTotal - SkillPointsLeft;

        [DataSourceProperty] public int SkillCap => CharacterWrapper.SkillCapsByTier[_troop.Tier];

        [DataSourceProperty] public string Athletics => _troop.Athletics.ToString();

        [DataSourceProperty] public string Riding => _troop.Riding.ToString();

        [DataSourceProperty] public string OneHanded => _troop.OneHanded.ToString();

        [DataSourceProperty] public string TwoHanded => _troop.TwoHanded.ToString();

        [DataSourceProperty] public string Polearm => _troop.Polearm.ToString();

        [DataSourceProperty] public string Bow => _troop.Bow.ToString();

        [DataSourceProperty] public string Crossbow => _troop.Crossbow.ToString();

        [DataSourceProperty] public string Throwing => _troop.Throwing.ToString();

        [DataSourceProperty] public string BannerCodeText => _troop.ViewModel.BannerCodeText;

        [DataSourceProperty] public string CharStringId => _troop.StringId;

        [DataSourceProperty] public string EquipmentCode => _troop.ViewModel.EquipmentCode;

        [DataSourceProperty] public string BodyProperties => _troop.ViewModel.BodyProperties;

        [DataSourceProperty] public bool IsFemale => _troop.ViewModel.IsFemale;

        [DataSourceProperty] public string MountCreationKey => _troop.ViewModel.MountCreationKey;

        [DataSourceProperty] public int Race => _troop.ViewModel.Race;

        [DataSourceProperty] public int StanceIndex => _troop.ViewModel.StanceIndex;

        [DataSourceProperty] public uint ArmorColor1 => _troop.ViewModel.ArmorColor1;

        [DataSourceProperty] public uint ArmorColor2 => _troop.ViewModel.ArmorColor2;

        [DataSourceProperty] public bool CanIncrementAthletics => CanIncrement(DefaultSkills.Athletics);

        [DataSourceProperty] public bool CanDecrementAthletics => CanDecrement(DefaultSkills.Athletics);

        [DataSourceProperty] public bool CanIncrementRiding => CanIncrement(DefaultSkills.Riding);

        [DataSourceProperty] public bool CanDecrementRiding => CanDecrement(DefaultSkills.Riding);

        [DataSourceProperty] public bool CanIncrementOneHanded => CanIncrement(DefaultSkills.OneHanded);

        [DataSourceProperty] public bool CanDecrementOneHanded => CanDecrement(DefaultSkills.OneHanded);

        [DataSourceProperty] public bool CanIncrementTwoHanded => CanIncrement(DefaultSkills.TwoHanded);

        [DataSourceProperty] public bool CanDecrementTwoHanded => CanDecrement(DefaultSkills.TwoHanded);

        [DataSourceProperty] public bool CanIncrementPolearm => CanIncrement(DefaultSkills.Polearm);

        [DataSourceProperty] public bool CanDecrementPolearm => CanDecrement(DefaultSkills.Polearm);

        [DataSourceProperty] public bool CanIncrementBow => CanIncrement(DefaultSkills.Bow);

        [DataSourceProperty] public bool CanDecrementBow => CanDecrement(DefaultSkills.Bow);

        [DataSourceProperty] public bool CanIncrementCrossbow => CanIncrement(DefaultSkills.Crossbow);

        [DataSourceProperty] public bool CanDecrementCrossbow => CanDecrement(DefaultSkills.Crossbow);

        [DataSourceProperty] public bool CanIncrementThrowing => CanIncrement(DefaultSkills.Throwing);

        [DataSourceProperty] public bool CanDecrementThrowing => CanDecrement(DefaultSkills.Throwing);

        private bool CanIncrement(SkillObject skill)
        {
            if (_troop == null) return false;

            var belowCap = _troop.Skills.FirstOrDefault(s => s.skill == skill).value < CharacterWrapper.SkillCapsByTier[_troop.Tier];
            var hasPointsLeft = _owner.TroopEditor.SkillPointsLeft > 0;

            return belowCap && hasPointsLeft;
        }

        private bool CanDecrement(SkillObject skill)
        {
            if (_troop == null) return false;

            return _troop.Skills.FirstOrDefault(s => s.skill == skill).value > 0;
        }

        private void ModifySkill(SkillObject skill, int change)
        {
            int repeat = 1;
            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                repeat = 500;
            else if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                repeat = 5;
            for (int i = 0; i < repeat; i++)
            {
                if (change > 0 && !CanIncrement(skill)) break;
                if (change < 0 && !CanDecrement(skill)) break;

                var skills = _troop.Skills;
                var idx = skills.FindIndex(s => s.skill == skill);
                if (idx >= 0)
                {
                    skills[idx] = (skill, skills[idx].value + change);
                    _troop.Skills = skills;
                }
            }
            _owner.UpdateTroops();
        }

        [DataSourceMethod] public void ExecuteIncrementAthletics() => ModifySkill(DefaultSkills.Athletics, 1);

        [DataSourceMethod] public void ExecuteDecrementAthletics() => ModifySkill(DefaultSkills.Athletics, -1);

        [DataSourceMethod] public void ExecuteIncrementRiding() => ModifySkill(DefaultSkills.Riding, 1);

        [DataSourceMethod] public void ExecuteDecrementRiding() => ModifySkill(DefaultSkills.Riding, -1);

        [DataSourceMethod] public void ExecuteIncrementOneHanded() => ModifySkill(DefaultSkills.OneHanded, 1);

        [DataSourceMethod] public void ExecuteDecrementOneHanded() => ModifySkill(DefaultSkills.OneHanded, -1);

        [DataSourceMethod] public void ExecuteIncrementTwoHanded() => ModifySkill(DefaultSkills.TwoHanded, 1);

        [DataSourceMethod] public void ExecuteDecrementTwoHanded() => ModifySkill(DefaultSkills.TwoHanded, -1);

        [DataSourceMethod] public void ExecuteIncrementPolearm() => ModifySkill(DefaultSkills.Polearm, 1);

        [DataSourceMethod] public void ExecuteDecrementPolearm() => ModifySkill(DefaultSkills.Polearm, -1);

        [DataSourceMethod] public void ExecuteIncrementBow() => ModifySkill(DefaultSkills.Bow, 1);

        [DataSourceMethod] public void ExecuteDecrementBow() => ModifySkill(DefaultSkills.Bow, -1);

        [DataSourceMethod] public void ExecuteIncrementCrossbow() => ModifySkill(DefaultSkills.Crossbow, 1);

        [DataSourceMethod] public void ExecuteDecrementCrossbow() => ModifySkill(DefaultSkills.Crossbow, -1);

        [DataSourceMethod] public void ExecuteIncrementThrowing() => ModifySkill(DefaultSkills.Throwing, 1);

        [DataSourceMethod] public void ExecuteDecrementThrowing() => ModifySkill(DefaultSkills.Throwing, -1);

        [DataSourceMethod] public void ExecuteAddUpgradeTarget()
        {
            var data = new TextInquiryData(
                new TextObject("Create Troop").ToString(),
                new TextObject("Enter the troop's name:").ToString(),
                true,
                true,
                new TextObject("{=OK}OK").ToString(),
                new TextObject("{=Cancel}Cancel").ToString(),
                (string input) =>
                {
                    input = (input ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(input)) return;

                    var target = CharacterHelpers.CloneTroop(_troop, input);
                    target.Level = _troop.Level + 5;

                    _troop.AddUpgradeTarget(target);

                    if (IsElite)
                    {
                        TroopManager.AddEliteTroop(target);
                    }
                    else
                    {
                        TroopManager.AddBasicTroop(target);
                    }

                    _owner.UpdateTroops();
                    _owner.UpdateLists();

                    Log.Debug($"TroopEditorVM: created upgrade target → '{input}'");
                },
                null,               // onNegative
                false,              // isInputObfuscated (arg #9 MUST be bool)
                (string input) =>   // validator (arg #10)
                {
                    bool ok = !string.IsNullOrWhiteSpace(input?.Trim());
                    return new System.Tuple<bool, string>(ok, ok ? "" : "Name cannot be empty.");
                }
            );

            InformationManager.ShowTextInquiry(data);
        }

        [DataSourceMethod] public void ExecuteChangeGenderSelected()
        {
            _troop.IsFemale = !_troop.IsFemale;

            _owner.UpdateTroops();
            Log.Debug($"TroopEditorVM: changed gender of '{_troop.Name}' to '{(_troop.IsFemale ? "Female" : "Male")}'");
        }

        [DataSourceMethod] public void ExecuteRenameSelected()
        {
            var current = _troop.Name;

            var data = new TextInquiryData(
                new TextObject("Rename Troop").ToString(),
                new TextObject("Enter the new name:").ToString(),
                true,
                true,
                new TextObject("{=OK}OK").ToString(),
                new TextObject("{=Cancel}Cancel").ToString(),
                (string input) =>
                {
                    input = (input ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(input)) return;

                    // Apply to our OO model and push to engine
                    _troop.Name = input;

                    _owner.UpdateTroops();
                    Log.Debug($"TroopEditorVM: renamed '{current}' → '{input}'");
                },
                null,               // onNegative
                false,              // isInputObfuscated (arg #9 MUST be bool)
                (string input) =>   // validator (arg #10)
                {
                    bool ok = !string.IsNullOrWhiteSpace(input?.Trim());
                    return new System.Tuple<bool, string>(ok, ok ? "" : "Name cannot be empty.");
                },
                current             // defaultInputText (arg #11)
            );

            InformationManager.ShowTextInquiry(data);
        }

        [DataSourceMethod] public void ExecuteRemoveTroop()
        {
            var troopName = _troop.Name;
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Remove Troop?",
                    $"Are you sure you want to remove '{troopName}'? This cannot be undone.",
                    true,
                    true,
                    "Yes",
                    "No",
                    () => {
                        TroopManager.RemoveTroop(_troop);
                        _owner.UpdateLists();
                        _owner.SelectFirstTroop();
                        Log.Debug($"TroopEditorVM: removed troop '{troopName}'");
                    },
                    null
                )
            );
        }

        [DataSourceMethod] public void ExecuteEditEquipment()
        {
            Log.Info($"TroopEditorVM: editing equipment for '{_troop.Name}'");
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Gender));

            // Upgrade target properties
            OnPropertyChanged(nameof(CanRemoveTroop));
            OnPropertyChanged(nameof(CanAddUpgradeTarget));
            OnPropertyChanged(nameof(HasUpgradeTarget1));
            OnPropertyChanged(nameof(UpgradeTarget1Id));
            OnPropertyChanged(nameof(UpgradeTarget1Name));
            OnPropertyChanged(nameof(HasUpgradeTarget2));
            OnPropertyChanged(nameof(UpgradeTarget2Id));
            OnPropertyChanged(nameof(UpgradeTarget2Name));

            // Visual model properties
            OnPropertyChanged(nameof(BannerCodeText));
            OnPropertyChanged(nameof(CharStringId));
            OnPropertyChanged(nameof(EquipmentCode));
            OnPropertyChanged(nameof(BodyProperties));
            OnPropertyChanged(nameof(IsFemale));
            OnPropertyChanged(nameof(MountCreationKey));
            OnPropertyChanged(nameof(Race));
            OnPropertyChanged(nameof(StanceIndex));
            OnPropertyChanged(nameof(ArmorColor1));
            OnPropertyChanged(nameof(ArmorColor2));

            // Skill properties    
            OnPropertyChanged(nameof(SkillPointsTotal));
            OnPropertyChanged(nameof(SkillPointsLeft));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(SkillCap));

            // Skill value properties
            OnPropertyChanged(nameof(Athletics));
            OnPropertyChanged(nameof(Riding));
            OnPropertyChanged(nameof(OneHanded));
            OnPropertyChanged(nameof(TwoHanded));
            OnPropertyChanged(nameof(Polearm));
            OnPropertyChanged(nameof(Bow));
            OnPropertyChanged(nameof(Crossbow));
            OnPropertyChanged(nameof(Throwing));

            // Skill update properties
            OnPropertyChanged(nameof(CanIncrementAthletics));
            OnPropertyChanged(nameof(CanDecrementAthletics));
            OnPropertyChanged(nameof(CanIncrementRiding));
            OnPropertyChanged(nameof(CanDecrementRiding));
            OnPropertyChanged(nameof(CanIncrementOneHanded));
            OnPropertyChanged(nameof(CanDecrementOneHanded));
            OnPropertyChanged(nameof(CanIncrementTwoHanded));
            OnPropertyChanged(nameof(CanDecrementTwoHanded));
            OnPropertyChanged(nameof(CanIncrementPolearm));
            OnPropertyChanged(nameof(CanDecrementPolearm));
            OnPropertyChanged(nameof(CanIncrementBow));
            OnPropertyChanged(nameof(CanDecrementBow));
            OnPropertyChanged(nameof(CanIncrementCrossbow));
            OnPropertyChanged(nameof(CanDecrementCrossbow));
            OnPropertyChanged(nameof(CanIncrementThrowing));
            OnPropertyChanged(nameof(CanDecrementThrowing));
        }
    }
}
