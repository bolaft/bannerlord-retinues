using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Editor.Controllers
{
    public class UpgradeController : BaseController
    {
        const int MaxUpgradeTargets = 4;

        /// <summary>
        /// Check if another upgrade target can be added.
        /// </summary>
        public static bool CanAddUpgradeTarget()
        {
            var character = State.Character;

            if (character.IsHero)
                return false; // Heroes cannot have upgrade targets.

            if (character.UpgradeTargets.Count >= MaxUpgradeTargets)
                return false; // Reached max upgrade targets.

            if (!character.InTree)
                return false; // Not in a tree.

            if (character.IsMaxTier)
                return false; // Already at max tier.

            return true;
        }

        /// <summary>
        /// Add a new upgrade target to the character.
        /// </summary>
        public static void AddUpgradeTarget()
        {
            if (!CanAddUpgradeTarget())
                return;

            var character = State.Character;

            void Apply(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Inquiries.Popup(
                        L.T("invalid_name_title", "Invalid Name"),
                        L.T("invalid_name_body", "The name cannot be empty.")
                    );
                    return;
                }

                var clone = character.Clone(skills: true, equipments: true);
                clone.Name = name;
                clone.Level = character.Level + 5;
                clone.HiddenInEncyclopedia = false;
                character.AddUpgradeTarget(clone);
                EventManager.Fire(UIEvent.Tree, EventScope.Global);
            }

            Inquiries.TextInputPopup(
                title: L.T("create_unit", "New Unit"),
                defaultInput: character.Name,
                onConfirm: input => Apply(input.Trim()),
                description: L.T("enter_name", "Enter a name:")
            );
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
