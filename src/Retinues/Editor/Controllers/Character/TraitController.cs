using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Editor.Controllers.Character
{
    public class TraitController : EditorController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<TraitObject> TraitIncrease { get; } =
            Action<TraitObject>("TraitIncrease")
                .AddCondition(
                    trait =>
                    {
                        // No behavior change: if not a hero, keep action enabled, execution is a no-op.
                        if (State.Character.Editable is not WHero hero)
                            return true;

                        return hero.GetTrait(trait) < trait.MaxValue;
                    },
                    L.T("trait_increase_maxed_reason", "Trait is already at maximum value.")
                )
                .ExecuteWith(trait => ChangeTrait(trait, +1))
                .Fire(UIEvent.Trait, EventScope.Global);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<TraitObject> TraitDecrease { get; } =
            Action<TraitObject>("TraitDecrease")
                .AddCondition(
                    trait =>
                    {
                        // No behavior change: if not a hero, keep action enabled, execution is a no-op.
                        if (State.Character.Editable is not WHero hero)
                            return true;

                        return hero.GetTrait(trait) > trait.MinValue;
                    },
                    L.T("trait_decrease_mined_reason", "Trait is already at minimum value.")
                )
                .ExecuteWith(trait => ChangeTrait(trait, -1))
                .Fire(UIEvent.Trait, EventScope.Global);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ChangeTrait(TraitObject trait, int delta)
        {
            if (State.Character.Editable is not WHero hero)
                return; // Only heroes can have traits.

            int current = hero.GetTrait(trait);
            int next = current + delta;

            // Bounds are enforced by UIAction conditions.
            hero.SetTrait(trait, next);
        }
    }
}
