using TaleWorlds.Core;
using TaleWorlds.Library;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI
{
    public class TroopInfoVM : ViewModel
    {
        private readonly CharacterWrapper _character;

        [DataSourceProperty]
        public string Gender => _character.IsFemale ? "Female" : "Male";

        [DataSourceProperty]
        public int SkillPointsTotal => CharacterWrapper.TotalSkillPointsByTier[_character.Tier];

        [DataSourceProperty]
        public int SkillPointsLeft
        {
            get
            {
                var points = SkillPointsTotal;

                foreach (var skill in CharacterWrapper.TroopSkills)
                {
                    points -= _character.GetSkill(skill);
                }

                return points;
            }
        }

        [DataSourceProperty]
        public int SkillPointsUsed => SkillPointsTotal - SkillPointsLeft;

        [DataSourceProperty]
        public int SkillCap => CharacterWrapper.SkillCapsByTier[_character.Tier];

        [DataSourceProperty]
        public string Athletics => _character.Athletics.ToString();
        [DataSourceProperty]
        public string Riding => _character.Riding.ToString();
        [DataSourceProperty]
        public string OneHanded => _character.OneHanded.ToString();
        [DataSourceProperty]
        public string TwoHanded => _character.TwoHanded.ToString();
        [DataSourceProperty]
        public string Polearm => _character.Polearm.ToString();
        [DataSourceProperty]
        public string Bow => _character.Bow.ToString();
        [DataSourceProperty]
        public string Crossbow => _character.Crossbow.ToString();
        [DataSourceProperty]
        public string Throwing => _character.Throwing.ToString();

        public TroopInfoVM(CharacterWrapper character)
        {
            _character = character;
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SkillPointsTotal));
            OnPropertyChanged(nameof(SkillPointsLeft));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(SkillCap));
            OnPropertyChanged(nameof(Athletics));
            OnPropertyChanged(nameof(Riding));
            OnPropertyChanged(nameof(OneHanded));
            OnPropertyChanged(nameof(TwoHanded));
            OnPropertyChanged(nameof(Polearm));
            OnPropertyChanged(nameof(Bow));
            OnPropertyChanged(nameof(Crossbow));
            OnPropertyChanged(nameof(Throwing));
        }
    }
}
