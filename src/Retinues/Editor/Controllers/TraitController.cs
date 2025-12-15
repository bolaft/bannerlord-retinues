using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace Retinues.Editor.Controllers
{
    public class TraitController : BaseController
    {
        /// <summary>
        /// Determines if the trait can be incremented.
        /// </summary>
        public static bool CanIncrement(TraitObject trait, int value) => value < trait.MaxValue;

        /// <summary>
        /// Determines if the trait can be decremented.
        /// </summary>
        public static bool CanDecrement(TraitObject trait, int value) => value > trait.MinValue;

        /// <summary>
        /// Changes the trait value by delta, if valid.
        /// </summary>
        public static void Change(TraitObject trait, int value, int delta)
        {
            if (State.Character.Editable is not WHero hero)
                return; // Only heroes can have traits.

            if (delta > 0)
                if (!CanIncrement(trait, value))
                    return;
            if (delta < 0)
                if (!CanDecrement(trait, value))
                    return;

            int current = hero.GetTrait(trait);
            int next = current + delta;

            hero.SetTrait(trait, next);

            EventManager.Fire(UIEvent.Trait, EventScope.Global);
        }
    }
}
