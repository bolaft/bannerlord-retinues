using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Character.Controllers;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Character.Views.Panel
{
    public class CharacterSkillVM(SkillObject skill) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Main                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Id => skill.StringId;

        [DataSourceProperty]
        public Tooltip Tooltip => new(skill.Name.ToString());

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public Tooltip StagingTooltip =>
            IsStaged
                ? new Tooltip(
                    L.T(
                            "skill_value_hint_staged",
                            "Actual skill value until training completes: {CURRENT}."
                        )
                        .SetTextVariable("CURRENT", State.Character.Skills.GetBase(skill))
                )
                : null;

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public int Value => State.Character.Editable.Skills.Get(skill);

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public bool IsStaged =>
            State.Character?.Editable is WCharacter wc && wc.Skills.IsStaged(skill);

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public string ValueColor => IsStaged ? "#ebaf2fff" : "#F4E1C4FF";

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
