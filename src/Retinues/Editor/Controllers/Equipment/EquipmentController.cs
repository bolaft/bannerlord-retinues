using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Equipments.Models;
using Retinues.UI.Services;

namespace Retinues.Editor.Controllers.Equipment
{
    /// <summary>
    /// Non-view logic for equipment set navigation and mutation.
    /// </summary>
    public class EquipmentController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Select Prev Set                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectPrevSet { get; } =
            Action<bool>("SelectPrevSet")
                .AddCondition(
                    _ => State.Character != null,
                    L.T("equipment_no_character_reason", "No character selected.")
                )
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i >= 0;
                    },
                    L.T("equipment_no_set_selected_reason", "No equipment set selected.")
                )
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i > 0;
                    },
                    L.T("equipment_no_more_sets", "No more equipment sets.")
                )
                .ExecuteWith(civilian =>
                {
                    var list = GetEquipments(civilian);
                    int i = IndexOfByBase(list, State.Equipment);
                    if (i <= 0)
                        return;

                    State.Equipment = list[i - 1];
                });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Select Next Set                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SelectNextSet { get; } =
            Action<bool>("SelectNextSet")
                .AddCondition(
                    _ => State.Character != null,
                    L.T("equipment_no_character_reason", "No character selected.")
                )
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i >= 0;
                    },
                    L.T("equipment_no_set_selected_reason", "No equipment set selected.")
                )
                .AddCondition(
                    civilian =>
                    {
                        var list = GetEquipments(civilian);
                        int i = IndexOfByBase(list, State.Equipment);
                        return i >= 0 && i < list.Count - 1;
                    },
                    L.T("equipment_no_more_sets", "No more equipment sets.")
                )
                .ExecuteWith(civilian =>
                {
                    var list = GetEquipments(civilian);
                    int i = IndexOfByBase(list, State.Equipment);
                    if (i < 0 || i >= list.Count - 1)
                        return;

                    State.Equipment = list[i + 1];
                });

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Create Set                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> CreateSet { get; } =
            Action<bool>("CreateSet")
                .AddCondition(
                    _ => State.Character != null,
                    L.T("equipment_no_character_reason", "No character selected.")
                )
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("equipment_hero_sets_reason", "Heroes cannot have multiple equipment sets.")
                )
                .DefaultTooltip(L.T("equipments_create_set", "Create a new equipment set."))
                .ExecuteWith(CreateSetImpl);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Delete Set                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> DeleteSet { get; } =
            Action<bool>("DeleteSet")
                .AddCondition(
                    _ => State.Character != null,
                    L.T("equipment_no_character_reason", "No character selected.")
                )
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("equipment_hero_sets_reason", "Heroes cannot have multiple equipment sets.")
                )
                .AddCondition(
                    civilian => GetEquipments(civilian).Count > 1,
                    L.T(
                        "equipment_cannot_delete_last_set",
                        "At least one equipment set must remain."
                    )
                )
                .DefaultTooltip(L.T("equipments_delete_set", "Delete the selected equipment set."))
                .ExecuteWith(DeleteSetImpl);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Queries                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<MEquipment> GetEquipments(bool civilian)
        {
            var all = State.Character?.Editable?.Equipments;
            if (all == null || all.Count == 0)
                return [];

            return civilian
                ? all.FindAll(e => e != null && e.IsCivilian)
                : all.FindAll(e => e != null && !e.IsCivilian);
        }

        public static int IndexOfByBase(List<MEquipment> list, MEquipment equipment)
        {
            if (list == null || list.Count == 0)
                return -1;

            var target = equipment?.Base;
            if (target == null)
                return -1;

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e?.Base == target)
                    return i;
            }

            return -1;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Mutations                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void SelectFirstOrPromptCreate(
            bool civilian,
            Action<MEquipment> applySelection,
            bool allowCreate
        )
        {
            if (applySelection == null)
                return;

            var character = State.Character;

            var list = GetEquipments(civilian);
            var first = list.FirstOrDefault();
            if (first != null)
            {
                applySelection(first);
                return;
            }

            if (!allowCreate || character == null || character.IsHero)
            {
                applySelection(null);
                return;
            }

            Inquiries.Popup(
                title: civilian
                    ? L.T("inquiry_no_civilian_sets", "No Civilian Equipments")
                    : L.T("inquiry_no_battle_sets", "No Battle Equipments"),
                description: L.T(
                        "inquiry_no_equipment_sets_text",
                        "{UNIT_NAME} has no {EQUIPMENT_TYPE}.\n\nCreate an empty one?"
                    )
                    .SetTextVariable(
                        "EQUIPMENT_TYPE",
                        civilian
                            ? L.T("inquiry_no_equipment_sets_civilian", "civilian equipments")
                            : L.T("inquiry_no_equipment_sets_battle", "battle equipments")
                    )
                    .SetTextVariable("UNIT_NAME", character.Name.ToString()),
                onConfirm: () =>
                {
                    var character = State.Character;
                    var created = MEquipment.Create(character, civilian: civilian);
                    character.EquipmentRoster.Add(created);
                    var refreshed = GetEquipments(civilian).FirstOrDefault();
                    applySelection(refreshed ?? created);
                }
            );
        }

        private static void CreateSetImpl(bool civilian)
        {
            void Apply(MEquipment source = null)
            {
                var character = State.Character;
                var created = MEquipment.Create(character, civilian: civilian, source: source);
                character.EquipmentRoster.Add(created);
                State.Equipment = created;
            }

            Inquiries.Popup(
                title: L.T("inquiry_confirm_create_equipment_set_title", "Create Equipment"),
                description: L.T(
                    "inquiry_confirm_create_equipment_set_text",
                    "Do you want to create a new equipment set by copying the current set or create an empty one?"
                ),
                choice1Text: L.T("inquiry_create_equipment_set_choice_copy", "Copy"),
                choice2Text: L.T("inquiry_create_equipment_set_choice_empty", "Empty"),
                onChoice1: () => Apply(source: State.Equipment),
                onChoice2: () => Apply()
            );
        }

        private static void DeleteSetImpl(bool civilian)
        {
            Inquiries.Popup(
                title: L.T("inquiry_confirm_delete_equipment_set_title", "Delete Equipment"),
                description: L.T(
                    "inquiry_confirm_delete_equipment_set_text",
                    "Are you sure you want to delete the current equipment set? This action cannot be undone."
                ),
                onConfirm: () =>
                {
                    State.Character.EquipmentRoster.Remove(State.Equipment);
                    State.Equipment = GetEquipments(civilian).FirstOrDefault();
                }
            );
        }
    }
}
