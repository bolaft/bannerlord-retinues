using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Editor.Events;
using Retinues.Editor.Services;
using Retinues.Modules;
using Retinues.UI.Services;
using TaleWorlds.Localization;

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
                .RequireValidEditingContext()
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
                .RequireValidEditingContext()
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
                .AddCondition(
                    civilian =>
                    {
                        if (civilian)
                            return true;

                        var target = State.Equipment;
                        return CanDeleteBattleEquipment(target);
                    },
                    L.T(
                        "equipment_delete_breaks_battle_types_reason",
                        "Cannot delete this set because it is the last one enabled for at least one battle type."
                    )
                )
                .DefaultTooltip(L.T("equipments_delete_set", "Delete the selected equipment set."))
                .ExecuteWith(DeleteSetImpl);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Crafted Items                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<bool> SetShowCrafted { get; } =
            Action<bool>("SetShowCrafted")
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("crafted_player_only_reason", "Only available in player mode.")
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T("crafted_items_only_tooltip", "Show crafted weapons.")
                        : L.T("crafted_items_hide_tooltip", "Hide crafted weapons.")
                )
                .ExecuteWith(value => State.ShowCrafted = value)
                .Fire(UIEvent.Crafted);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle Types                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private enum BattleType
        {
            Field,
            Siege,
            Naval,
        }

        private static IEnumerable<BattleType> RequiredBattleTypes()
        {
            yield return BattleType.Field;
            yield return BattleType.Siege;

            if (Mods.NavalDLC.IsLoaded)
                yield return BattleType.Naval;
        }

        private static bool GetBattleTypeValue(MEquipment e, BattleType type)
        {
            if (e == null)
                return false;

            return type switch
            {
                BattleType.Field => e.FieldBattleSet,
                BattleType.Siege => e.SiegeBattleSet,
                BattleType.Naval => e.NavalBattleSet,
                _ => false,
            };
        }

        private static void SetBattleTypeValue(MEquipment e, BattleType type, bool value)
        {
            if (e == null)
                return;

            switch (type)
            {
                case BattleType.Field:
                    e.FieldBattleSet = value;
                    break;
                case BattleType.Siege:
                    e.SiegeBattleSet = value;
                    break;
                case BattleType.Naval:
                    e.NavalBattleSet = value;
                    break;
            }
        }

        private static TextObject GetDisableReason(BattleType type)
        {
            return type switch
            {
                BattleType.Field => L.T(
                    "battle_type_field_required_reason",
                    "At least one battle equipment set must remain enabled for field battles."
                ),
                BattleType.Siege => L.T(
                    "battle_type_siege_required_reason",
                    "At least one battle equipment set must remain enabled for siege battles."
                ),
                BattleType.Naval => L.T(
                    "battle_type_naval_required_reason",
                    "At least one battle equipment set must remain enabled for naval battles."
                ),
                _ => L.T(
                    "battle_type_required_reason",
                    "At least one battle equipment set must remain enabled for this battle type."
                ),
            };
        }

        private static bool CoverageSatisfiedAfterChange(
            List<MEquipment> battleEquipments,
            MEquipment changing,
            BattleType type,
            bool newValue
        )
        {
            if (type == BattleType.Naval && !Mods.NavalDLC.IsLoaded)
                return true;

            if (battleEquipments == null || battleEquipments.Count == 0)
                return false;

            var changingBase = changing?.Base;

            for (int i = 0; i < battleEquipments.Count; i++)
            {
                var e = battleEquipments[i];
                if (e == null || e.IsCivilian)
                    continue;

                bool value = GetBattleTypeValue(e, type);

                if (changingBase != null && e.Base == changingBase)
                    value = newValue;

                if (value)
                    return true;
            }

            return false;
        }

        private static bool CanDisableBattleType(MEquipment equipment, BattleType type)
        {
            if (equipment == null || equipment.IsCivilian)
                return false;

            if (!GetBattleTypeValue(equipment, type))
                return true;

            var battle = GetEquipments(civilian: false);
            return CoverageSatisfiedAfterChange(battle, equipment, type, newValue: false);
        }

        public static TextObject GetFieldBattleDisableReason()
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.FieldBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Field)
                ? null
                : GetDisableReason(BattleType.Field);
        }

        public static TextObject GetSiegeBattleDisableReason()
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.SiegeBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Siege)
                ? null
                : GetDisableReason(BattleType.Siege);
        }

        public static TextObject GetNavalBattleDisableReason()
        {
            if (!Mods.NavalDLC.IsLoaded)
                return null;

            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return null;

            if (!e.NavalBattleSet)
                return null;

            return CanDisableBattleType(e, BattleType.Naval)
                ? null
                : GetDisableReason(BattleType.Naval);
        }

        public static bool CanDeleteBattleEquipment(MEquipment equipment)
        {
            if (equipment == null || equipment.IsCivilian)
                return true;

            var battle = GetEquipments(civilian: false);
            if (battle == null || battle.Count <= 1)
                return false;

            var targetBase = equipment.Base;
            var remaining = battle.Where(e => e != null && e.Base != targetBase).ToList();

            foreach (var type in RequiredBattleTypes())
            {
                bool any = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var e = remaining[i];
                    if (e == null || e.IsCivilian)
                        continue;

                    if (GetBattleTypeValue(e, type))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                    return false;
            }

            return true;
        }

        public static EditorAction<bool> SetFieldBattleSet { get; } =
            Action<bool>("SetFieldBattleSet")
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Field),
                    GetDisableReason(BattleType.Field)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_field_checkbox_tooltip_enable",
                            "Enable for field battles."
                        )
                        : L.T(
                            "battle_type_field_checkbox_tooltip_disable",
                            "Disable for field battles."
                        )
                )
                .ExecuteWith(SetFieldBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetFieldBattleSetImpl(bool value)
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Field))
                return;

            SetBattleTypeValue(e, BattleType.Field, value);
        }

        public static EditorAction<bool> SetSiegeBattleSet { get; } =
            Action<bool>("SetSiegeBattleSet")
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Siege),
                    GetDisableReason(BattleType.Siege)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_siege_checkbox_tooltip_enable",
                            "Enable for siege battles."
                        )
                        : L.T(
                            "battle_type_siege_checkbox_tooltip_disable",
                            "Disable for siege battles."
                        )
                )
                .ExecuteWith(SetSiegeBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetSiegeBattleSetImpl(bool value)
        {
            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Siege))
                return;

            SetBattleTypeValue(e, BattleType.Siege, value);
        }

        public static EditorAction<bool> SetNavalBattleSet { get; } =
            Action<bool>("SetNavalBattleSet")
                .AddCondition(
                    _ => Mods.NavalDLC.IsLoaded,
                    L.T("naval_dlc_not_loaded", "War Sails is not installed.")
                )
                .AddCondition(
                    _ => State.Equipment != null && State.Equipment.IsCivilian == false,
                    L.T(
                        "battle_types_civilian_reason",
                        "Civilian equipment sets do not have battle type restrictions."
                    )
                )
                .AddCondition(
                    value => value || CanDisableBattleType(State.Equipment, BattleType.Naval),
                    GetDisableReason(BattleType.Naval)
                )
                .DefaultTooltip(value =>
                    value
                        ? L.T(
                            "battle_type_naval_checkbox_tooltip_enable",
                            "Enable for naval battles."
                        )
                        : L.T(
                            "battle_type_naval_checkbox_tooltip_disable",
                            "Disable for naval battles."
                        )
                )
                .ExecuteWith(SetNavalBattleSetImpl)
                .Fire(UIEvent.BattleToggle);

        private static void SetNavalBattleSetImpl(bool value)
        {
            if (!Mods.NavalDLC.IsLoaded)
                return;

            var e = State.Equipment;
            if (e == null || e.IsCivilian)
                return;

            if (!value && !CanDisableBattleType(e, BattleType.Naval))
                return;

            SetBattleTypeValue(e, BattleType.Naval, value);
        }

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
                    // Economy is only active in player mode, when the setting is enabled, and preview is off.
                    bool economyActive =
                        State.Mode == EditorMode.Player
                        && Settings.EquipmentCostsMoney
                        && !PreviewController.Enabled;

                    if (!economyActive)
                    {
                        State.Character.EquipmentRoster.Remove(State.Equipment);
                        State.Equipment = GetEquipments(civilian).FirstOrDefault();
                        return;
                    }

                    var roster = State.Character?.EquipmentRoster;

                    StocksHelper.TrackRosterStock(
                        roster,
                        () =>
                        {
                            State.Character.EquipmentRoster.Remove(State.Equipment);
                            State.Equipment = GetEquipments(civilian).FirstOrDefault();
                        }
                    );
                }
            );
        }
    }
}
