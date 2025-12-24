using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Character;
using Retinues.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    public class CharacterSkillVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly SkillObject _skill;
        private readonly Tooltip _hint;

        public CharacterSkillVM(SkillObject skill)
        {
            _skill = skill;
            _hint = new Tooltip(_skill.Name.ToString());
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

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public bool CanIncrease => SkillsController.SkillIncrease.Allow(_skill);

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public Tooltip TooltipIncrease => SkillsController.SkillIncrease.Tooltip(_skill);

        [DataSourceMethod]
        public void ExecuteIncrease() => SkillsController.SkillIncrease.Execute(_skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public bool CanDecrease => SkillsController.SkillDecrease.Allow(_skill);

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public Tooltip TooltipDecrease => SkillsController.SkillDecrease.Tooltip(_skill);

        [DataSourceMethod]
        public void ExecuteDecrease() => SkillsController.SkillDecrease.Execute(_skill);
    }
}
