using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
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
    
        [DataSourceMethod]
        public void ExecuteChangeGenderSelected()
        {
            // Toggle gender
            _troop.SetIsFemale(!_troop.IsFemale);

            _owner.NotifyTroopChanged();
            Log.Debug($"TroopEditorVM: changed gender of '{_troop.Name}' to '{(_troop.IsFemale ? "Female" : "Male")}'");
        }

        [DataSourceMethod]
        public void ExecuteRenameSelected()
        {
            var current = _troop.Name;

            // BL 1.2.12 overload expects:
            // (title, text, showAff, showNeg, affText, negText,
            //  Action<string> onAffirmative, Action onNegative,
            //  bool isInputObfuscated,
            //  Func<string, Tuple<bool,string>> validator,
            //  string defaultInputText)
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
                    _troop.SetName(input);

                    _owner.NotifyTroopChanged();
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

        private void ModifySkill(SkillObject skill, int change)
        {
            if (_owner.TroopInfo.SkillPointsLeft < 1 && change > 0) return;

            int minValue = 0;
            int maxValue = CharacterWrapper.SkillCapsByTier[_troop.Tier];

            int newValue = _troop.GetSkill(skill) + change;
            if (newValue < minValue || newValue > maxValue) return;

            _troop.SetSkill(skill, newValue);
            _owner.NotifyTroopChanged();
        }

        [DataSourceMethod]
        public void ExecuteIncrementAthletics()
        {
            ModifySkill(DefaultSkills.Athletics, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementAthletics()
        {
            ModifySkill(DefaultSkills.Athletics, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementRiding()
        {
            ModifySkill(DefaultSkills.Riding, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementRiding()
        {
            ModifySkill(DefaultSkills.Riding, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementOneHanded()
        {
            ModifySkill(DefaultSkills.OneHanded, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementOneHanded()
        {
            ModifySkill(DefaultSkills.OneHanded, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementTwoHanded()
        {
            ModifySkill(DefaultSkills.TwoHanded, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementTwoHanded()
        {
            ModifySkill(DefaultSkills.TwoHanded, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementPolearm()
        {
            ModifySkill(DefaultSkills.Polearm, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementPolearm()
        {
            ModifySkill(DefaultSkills.Polearm, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementBow()
        {
            ModifySkill(DefaultSkills.Bow, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementBow()
        {
            ModifySkill(DefaultSkills.Bow, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementCrossbow()
        {
            ModifySkill(DefaultSkills.Crossbow, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementCrossbow()
        {
            ModifySkill(DefaultSkills.Crossbow, -1);
        }

        [DataSourceMethod]
        public void ExecuteIncrementThrowing()
        {
            ModifySkill(DefaultSkills.Throwing, 1);
        }

        [DataSourceMethod]
        public void ExecuteDecrementThrowing()
        {
            ModifySkill(DefaultSkills.Throwing, -1);
        }
    }
}
