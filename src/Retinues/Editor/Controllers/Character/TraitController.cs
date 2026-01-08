using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Editor.Controllers.Character
{
    public class TraitController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<TraitObject> TraitIncrease { get; } =
            Action<TraitObject>("TraitIncrease")
                .AddCondition(
                    _ => State.Character.Editable is WHero,
                    L.T("trait_only_heroes_reason", "Only heroes have traits.")
                )
                .AddCondition(
                    trait => State.Character.Hero.GetTrait(trait) < trait.MaxValue,
                    L.T("trait_increase_maxed_reason", "Trait is already at maximum value.")
                )
                .ExecuteWith(trait => ChangeTrait(trait, +1))
                .Fire(UIEvent.Trait);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<TraitObject> TraitDecrease { get; } =
            Action<TraitObject>("TraitDecrease")
                .AddCondition(
                    _ => State.Character.Editable is WHero,
                    L.T("trait_only_heroes_reason", "Only heroes have traits.")
                )
                .AddCondition(
                    trait => State.Character.Hero.GetTrait(trait) > trait.MinValue,
                    L.T("trait_decrease_mined_reason", "Trait is already at minimum value.")
                )
                .ExecuteWith(trait => ChangeTrait(trait, -1))
                .Fire(UIEvent.Trait);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Change the trait by the given delta.
        /// </summary>
        private static void ChangeTrait(TraitObject trait, int delta)
        {
            if (State.Character.Editable is not WHero hero)
                return;

            int current = hero.GetTrait(trait);
            hero.SetTrait(trait, current + delta);
        }
    }
}
