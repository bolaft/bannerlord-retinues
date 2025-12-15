using Retinues.Model.Characters;

namespace Retinues.Editor.Controllers
{
    public class UpgradeController : BaseController
    {
        const int MaxUpgradeTargets = 4;

        /// <summary>
        /// Check if another upgrade target can be added.
        /// </summary>
        public static bool CanAddUpgradeTarget() =>
            State.Character.UpgradeTargets.Count < MaxUpgradeTargets;

        /// <summary>
        /// Add a new upgrade target to the character.
        /// </summary>
        public static void AddUpgradeTarget()
        {
            if (!CanAddUpgradeTarget())
                return;

            var character = State.Character;

            character.AddUpgradeTarget(character.Clone(skills: true, equipments: true));
            EventManager.Fire(UIEvent.Tree, EventScope.Global);
        }

        /// <summary>
        /// Remove an upgrade target from the character.
        /// </summary>
        public static void RemoveUpgradeTarget(WCharacter target)
        {
            if (State.Character.RemoveUpgradeTarget(target))
                EventManager.Fire(UIEvent.Tree, EventScope.Global);
        }
    }
}
