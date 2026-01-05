using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
using Retinues.UI.Services;

namespace Retinues.Editor.Controllers.Character
{
    public class CharacterTreeController : BaseController
    {
        const int MaxUpgradeTargets = 4;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Add Upgrade                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> AddUpgradeTarget { get; } =
            Action<WCharacter>("AddUpgradeTarget")
                .AddCondition(
                    c => c != null && !c.IsHero,
                    L.T("upgrade_cannot_add_hero", "Heroes cannot have upgrade targets.")
                )
                .AddCondition(
                    c => c != null && c.UpgradeTargets.Count < MaxUpgradeTargets,
                    L.T(
                        "upgrade_cannot_add_max_targets",
                        "Maximum number of upgrade targets reached."
                    )
                )
                .AddCondition(
                    c => c != null && c.InTree,
                    L.T("upgrade_cannot_add_not_in_tree", "This unit is not in a troop tree.")
                )
                .AddCondition(
                    c => c != null && !c.IsMaxTier,
                    L.T("upgrade_cannot_add_max_tier", "This unit is already at maximum tier.")
                )
                .ExecuteWith(AddUpgradeTargetImpl);

        private static void AddUpgradeTargetImpl(WCharacter character)
        {
            if (!AddUpgradeTarget.Allow(character))
                return;

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

                bool copyEquipments = State.Mode != EditorMode.Player;
                var clone = character.Clone(equipments: copyEquipments);
                clone.Name = name.Trim();
                clone.Level = character.Level + 5;
                clone.HiddenInEncyclopedia = false;

                character.AddUpgradeTarget(clone);
                EventManager.Fire(UIEvent.Tree);
            }

            Inquiries.TextInputPopup(
                title: L.T("create_unit", "New Unit"),
                defaultInput: character.Name,
                onConfirm: Apply,
                description: L.T("enter_name", "Enter a name:")
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Remove Character                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> RemoveCharacter { get; } =
            Action<WCharacter>("RemoveCharacter")
                .AddCondition(
                    c => c != null && c.IsHero == false,
                    L.T("character_cannot_remove_hero", "Heroes cannot be removed.")
                )
                .AddCondition(
                    c => c != null && c.UpgradeTargets.Count == 0,
                    L.T(
                        "character_cannot_remove_with_upgrades",
                        "Can't remove a unit that still has upgrades."
                    )
                )
                .AddCondition(
                    c => c != null && !c.IsRoot,
                    L.T("character_cannot_remove_root", "Root units cannot be removed.")
                )
                .ExecuteWith(RemoveCharacterImpl);

        private static void RemoveCharacterImpl(WCharacter character)
        {
            if (!RemoveCharacter.Allow(character))
                return;

            Inquiries.Popup(
                title: L.T("inquiry_confirm_remove_character_title", "Delete Unit"),
                description: L.T(
                        "inquiry_confirm_remove_character_text",
                        "Are you sure you want to delete {UNIT_NAME}? This action cannot be undone."
                    )
                    .SetTextVariable("UNIT_NAME", character.Name.ToString()),
                onConfirm: () =>
                {
                    if (ItemController.EconomyActive)
                        ItemController.StockCharacterRoster(character);

                    State.Character = State.Faction.Troops.FirstOrDefault(c => c != character);
                    character.Remove();
                    EventManager.Fire(UIEvent.Tree);
                }
            );
        }
    }
}
