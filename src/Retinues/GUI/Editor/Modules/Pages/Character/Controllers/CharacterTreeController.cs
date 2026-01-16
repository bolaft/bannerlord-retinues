using System.Linq;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Equipment.Controllers;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Controllers
{
    public class CharacterTreeController : BaseController
    {
        const int MaxUpgradeTargets = 4;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Add Upgrade                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ControllerAction<WCharacter> AddUpgradeTarget { get; } =
            Action<WCharacter>("AddUpgradeTarget")
                .RequireValidEditingContext()
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

        /// <summary>
        /// Add an upgrade target to the given character.
        /// </summary>
        private static void AddUpgradeTargetImpl(WCharacter character)
        {
            if (!AddUpgradeTarget.Allow(character))
                return;

            void ApplyName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Inquiries.Popup(
                        L.T("invalid_name_title", "Invalid Name"),
                        L.T("invalid_name_body", "The name cannot be empty.")
                    );
                    return;
                }

                name = name.Trim();

                // Universal mode: keep current behavior unchanged.
                if (State.Mode != EditorMode.Player)
                {
                    var cloneU = CharacterCloner.Clone(character, equipments: true);
                    cloneU.Name = name;
                    cloneU.Level = character.Level + 5;
                    cloneU.HiddenInEncyclopedia = false;

                    character.AddUpgradeTarget(cloneU);
                    EventManager.Fire(UIEvent.Tree);
                    return;
                }

                // Player mode: ask how to initialize equipment.
                Inquiries.Popup(
                    title: L.T("create_unit_mode_title", "Create Unit"),
                    description: L.T(
                        "create_unit_mode_desc",
                        "How should the new unit's equipment be initialized?"
                    ),
                    onChoice1: () => CreateUpgrade_Player_Empty(character, name),
                    onChoice2: () => CreateUpgrade_Player_BestFromStocks(character, name),
                    choice1Text: L.T("create_unit_mode_empty", "No Equipment"),
                    choice2Text: L.T("create_unit_mode_best_stock", "From Stocks")
                );
            }

            Inquiries.TextInputPopup(
                title: L.T("create_unit", "New Unit"),
                defaultInput: character.Name,
                onConfirm: ApplyName,
                description: L.T("enter_name", "Enter a name:")
            );
        }

        /// <summary>
        /// Create an upgrade target with empty equipment (Player mode).
        /// </summary>
        private static void CreateUpgrade_Player_Empty(WCharacter parent, string name)
        {
            var clone = CharacterCloner.Clone(parent, equipments: false);
            clone.Name = name;
            clone.Level = parent.Level + 5;
            clone.HiddenInEncyclopedia = false;

            parent.AddUpgradeTarget(clone);
            EventManager.Fire(UIEvent.Tree);
        }

        /// <summary>
        /// Create an upgrade target with best-from-stock equipment (Player mode).
        /// </summary>
        private static void CreateUpgrade_Player_BestFromStocks(WCharacter parent, string name)
        {
            // Create the troop with no copied equipments first.
            var clone = CharacterCloner.Clone(parent, equipments: false);
            clone.Name = name;
            clone.Level = parent.Level + 5;
            clone.HiddenInEncyclopedia = false;

            // Build two sets: battle + civilian, best-from-stock.
            // (This does not consume stock by itself. We consume after the sets exist.)
            var roster = clone.EquipmentRoster;

            // Ensure a clean roster state. (Roster API differs across refactors; use what you have.)
            // We avoid calling Remove on existing sets while economy might be active; clone has none anyway.
            roster?.InvalidateItemCountsCache();

            var battle = RandomEquipmentHelper.CreateRandomEquipment(
                owner: clone,
                source: parent.FirstBattleEquipment,
                civilian: false,
                fromStocks: true,
                pickBest: true
            );

            var civilian = RandomEquipmentHelper.CreateRandomEquipment(
                owner: clone,
                source: parent.FirstCivilianEquipment,
                civilian: true,
                fromStocks: true,
                pickBest: true
            );

            roster.Add(battle);
            roster.Add(civilian);

            // Consume the conceptual requirement from stock (max-per-equipment logic).
            // Only do this when economy is active.
            if (ItemController.EconomyActive)
                StocksHelper.ConsumeStock(roster.ItemCountsById);

            parent.AddUpgradeTarget(clone);

            EventManager.Fire(UIEvent.Tree);
            EventManager.Fire(UIEvent.Item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Remove Character                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static ControllerAction<WCharacter> RemoveCharacter { get; } =
            Action<WCharacter>("RemoveCharacter")
                .RequireValidEditingContext()
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

        /// <summary>
        /// Remove the given character after confirmation.
        /// </summary>
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
