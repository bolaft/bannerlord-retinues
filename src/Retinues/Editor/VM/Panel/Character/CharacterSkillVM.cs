using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Character skill card.
    /// </summary>
    public class CharacterSkillVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly SkillObject _skill;
        private readonly Tooltip _hint;

        public CharacterSkillVM(SkillObject skill)
        {
            _skill = skill;
            _hint = new Tooltip(_skill.Name.ToString());

            RefreshSkillIncrease();
            RefreshSkillDecrease();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Id => _skill.StringId;

        [DataSourceProperty]
        public Tooltip Hint => _hint;

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public int Value => State.Character.Editable.Skills.Get(_skill);

        [DataSourceProperty]
        public string ValueColor => "#F4E1C4FF";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanIncrease { get; set; }

        [DataSourceProperty]
        public Tooltip TooltipIncrease { get; set; }

        [EventListener(UIEvent.Skill)]
        public void RefreshSkillIncrease()
        {
            CanIncrease = SkillsController.CanIncreaseSkill(_skill, out var reason);
            TooltipIncrease = reason != null ? new Tooltip(reason) : null;
            OnPropertyChanged(nameof(CanIncrease));
            OnPropertyChanged(nameof(TooltipIncrease));
        }

        [DataSourceMethod]
        public void ExecuteIncrease() => SkillsController.IncreaseSkill(_skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanDecrease { get; set; }

        [DataSourceProperty]
        public Tooltip TooltipDecrease { get; set; }

        [EventListener(UIEvent.Skill)]
        private void RefreshSkillDecrease()
        {
            CanDecrease = SkillsController.CanDecreaseSkill(_skill, out var reason);
            TooltipDecrease = reason != null ? new Tooltip(reason) : null;
            OnPropertyChanged(nameof(CanDecrease));
            OnPropertyChanged(nameof(TooltipDecrease));
        }

        [DataSourceMethod]
        public void ExecuteDecrease() => SkillsController.DecreaseSkill(_skill);
    }
}
