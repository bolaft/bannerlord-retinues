using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for hero trait modifications and related UI actions.
    /// </summary>
    public class TraitController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the specified trait for the selected hero.
        /// </summary>
        public static ControllerAction<TraitObject> TraitIncrease { get; } =
            Action<TraitObject>("TraitIncrease")
                .AddCondition(
                    _ => State.Character != null && State.Character.IsHero,
                    new TextObject(string.Empty).SetTextVariable(
                        "REASON",
                        L.T("trait_only_heroes_reason", "Only applicable to heroes")
                    )
                )
                .AddCondition(trait => State.Character.Hero.GetTrait(trait) < trait.MaxValue)
                .ExecuteWith(trait => ChangeTrait(trait, +1))
                .Fire(UIEvent.Trait);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Decreases the specified trait for the selected hero.
        /// </summary>
        public static ControllerAction<TraitObject> TraitDecrease { get; } =
            Action<TraitObject>("TraitDecrease")
                .AddCondition(
                    _ => State.Character != null && State.Character.IsHero,
                    L.T("trait_only_heroes_reason", "Only applicable to heroes")
                )
                .AddCondition(trait => State.Character.Hero.GetTrait(trait) > trait.MinValue)
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
            var hero = State.Character?.Hero;
            if (hero == null)
                return;

            int current = hero.GetTrait(trait);
            hero.SetTrait(trait, current + delta);
        }
    }
}
