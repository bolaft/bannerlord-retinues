using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Helpers;
using Retinues.Model.Equipments;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers.Equipment
{
    /// <summary>
    /// Non-view logic for equipment set navigation and mutation.
    /// </summary>
    public class EquipmentController : EditorController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Actions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Selects the previous equipment set of the given type.
        /// </summary>
        public static EditorAction<bool> SelectPrevSet { get; } =
            Action<bool>("SelectPrevSet")
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

        /// <summary>
        /// Selects the next equipment set of the given type.
        /// </summary>
        public static EditorAction<bool> SelectNextSet { get; } =
            Action<bool>("SelectNextSet")
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

        /// <summary>
        /// Creates a new equipment set (copy or empty) and selects it.
        /// </summary>
        public static EditorAction<bool> CreateSet { get; } =
            Action<bool>("CreateSet")
                .DefaultTooltip(L.T("equipments_create_set", "Create a new equipment set."))
                .ExecuteWith(CreateSetImpl);

        /// <summary>
        /// Deletes the currently selected equipment set of the given type.
        /// </summary>
        public static EditorAction<bool> DeleteSet { get; } =
            Action<bool>("DeleteSet")
                .DefaultTooltip(L.T("equipments_delete_set", "Delete the selected equipment set."))
                .AddCondition(
                    civilian => GetEquipments(civilian).Count > 1,
                    L.T(
                        "equipment_cannot_delete_last_set",
                        "At least one equipment set must remain."
                    )
                )
                .ExecuteWith(DeleteSetImpl);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Queries                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets all equipment sets of the given type for the current character.
        /// </summary>
        public static List<MEquipment> GetEquipments(bool civilian)
        {
            var all = State.Character.Editable.Equipments;
            if (all.Count == 0)
                return [];

            return civilian
                ? all.FindAll(e => e != null && e.IsCivilian)
                : all.FindAll(e => e != null && !e.IsCivilian);
        }

        /// <summary>
        /// Finds the index of the given equipment in the provided list.
        /// </summary>
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
        //                        Mutations                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Selects the first equipment set of the given type for the current character.
        /// If none exists, prompts to create a new one if allowed.
        /// </summary>
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
                                .SetTextVariable("UNIT_NAME", character.Name.ToString())
                    ),
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
