using Retinues.Editor.Controllers.Character;
using Retinues.Editor.Events;
using Retinues.UI.VM;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    public class CharacterSkillVM(SkillObject skill) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Id => skill.StringId;

        [DataSourceProperty]
        public Tooltip Hint => new(skill.Name.ToString());

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public int Value => State.Character.Editable.Skills.Get(skill);

        [DataSourceProperty]
        public string ValueColor => "#F4E1C4FF";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Buttons                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<SkillObject> IncreaseButton { get; } =
            new(action: SkillsController.SkillIncrease, arg: () => skill, refresh: UIEvent.Skill);

        [DataSourceProperty]
        public Button<SkillObject> DecreaseButton { get; } =
            new(action: SkillsController.SkillDecrease, arg: () => skill, refresh: UIEvent.Skill);
    }
}
