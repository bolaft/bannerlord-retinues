using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Missions.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.List;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.GUI.Helpers;
using Retinues.Mods;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for the equipment editor screen, containing slots and list.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentScreenVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Faction] =
                [
                    nameof(Weapon1Slot),
                    nameof(Weapon2Slot),
                    nameof(Weapon3Slot),
                    nameof(Weapon4Slot),
                    nameof(HeadSlot),
                    nameof(CapeSlot),
                    nameof(BodySlot),
                    nameof(GlovesSlot),
                    nameof(LegSlot),
                    nameof(HorseSlot),
                    nameof(HorseHarnessSlot),
                    nameof(CanUnstage),
                    nameof(CanUnequip),
                    nameof(EquipmentName),
                    nameof(CanSelectPrevSet),
                    nameof(CanSelectNextSet),
                    nameof(CanRemoveSet),
                    nameof(CanCreateSet),
                    nameof(RemoveSetHint),
                    nameof(CreateSetHint),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(Weapon1Slot),
                    nameof(Weapon2Slot),
                    nameof(Weapon3Slot),
                    nameof(Weapon4Slot),
                    nameof(HeadSlot),
                    nameof(CapeSlot),
                    nameof(BodySlot),
                    nameof(GlovesSlot),
                    nameof(LegSlot),
                    nameof(HorseSlot),
                    nameof(HorseHarnessSlot),
                    nameof(CanUnstage),
                    nameof(CanUnequip),
                    nameof(EquipmentName),
                    nameof(CanSelectPrevSet),
                    nameof(CanSelectNextSet),
                    nameof(CanRemoveSet),
                    nameof(CanCreateSet),
                    nameof(RemoveSetHint),
                    nameof(CreateSetHint),
                    nameof(SetIsCivilian),
                    nameof(SetIsBattle),
                    nameof(SetIsEnabledForFieldBattle),
                    nameof(SetIsEnabledForSiegeDefense),
                    nameof(SetIsEnabledForSiegeAssault),
                    nameof(FieldBattleHint),
                    nameof(SiegeDefenseHint),
                    nameof(SiegeAssaultHint),
                    nameof(CivilianHint),
                    nameof(CanToggleCivilianSet),
                    nameof(CanToggleEnableForFieldBattle),
                    nameof(CanToggleEnableForSiegeDefense),
                    nameof(CanToggleEnableForSiegeAssault),
                ],
                [UIEvent.Equip] = [nameof(CanUnstage), nameof(CanUnequip)],
                [UIEvent.Slot] =
                [
                    nameof(CanUnstage),
                    nameof(CanShowCrafted),
                    nameof(ShowCrafted),
                    nameof(ShowCraftedHint),
                ],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public EquipmentListVM EquipmentList { get; set; } = new();

        [DataSourceProperty]
        public EquipmentSlotVM Weapon1Slot { get; set; } = new(EquipmentIndex.Weapon0);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon2Slot { get; set; } = new(EquipmentIndex.Weapon1);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon3Slot { get; set; } = new(EquipmentIndex.Weapon2);

        [DataSourceProperty]
        public EquipmentSlotVM Weapon4Slot { get; set; } = new(EquipmentIndex.Weapon3);

        [DataSourceProperty]
        public EquipmentSlotVM HeadSlot { get; set; } = new(EquipmentIndex.Head);

        [DataSourceProperty]
        public EquipmentSlotVM CapeSlot { get; set; } = new(EquipmentIndex.Cape);

        [DataSourceProperty]
        public EquipmentSlotVM BodySlot { get; set; } = new(EquipmentIndex.Body);

        [DataSourceProperty]
        public EquipmentSlotVM GlovesSlot { get; set; } = new(EquipmentIndex.Gloves);

        [DataSourceProperty]
        public EquipmentSlotVM LegSlot { get; set; } = new(EquipmentIndex.Leg);

        [DataSourceProperty]
        public EquipmentSlotVM HorseSlot { get; set; } = new(EquipmentIndex.Horse);

        [DataSourceProperty]
        public EquipmentSlotVM HorseHarnessSlot { get; set; } = new(EquipmentIndex.HorseHarness);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Dictionary<EquipmentIndex, EquipmentSlotVM> _slotsByIndex;

        private EquipmentIndex? _lastSlotIndex = State.Slot;

        private IEnumerable<EquipmentSlotVM> EquipmentSlots =>
            [
                Weapon1Slot,
                Weapon2Slot,
                Weapon3Slot,
                Weapon4Slot,
                HeadSlot,
                CapeSlot,
                BodySlot,
                GlovesSlot,
                LegSlot,
                HorseSlot,
                HorseHarnessSlot,
            ];

        /// <summary>
        /// Initialize the equipment screen and index slot view-models by equipment slot.
        /// </summary>
        public EquipmentScreenVM()
        {
            _slotsByIndex = new Dictionary<EquipmentIndex, EquipmentSlotVM>(13)
            {
                [EquipmentIndex.Weapon0] = Weapon1Slot,
                [EquipmentIndex.Weapon1] = Weapon2Slot,
                [EquipmentIndex.Weapon2] = Weapon3Slot,
                [EquipmentIndex.Weapon3] = Weapon4Slot,
                [EquipmentIndex.Head] = HeadSlot,
                [EquipmentIndex.Cape] = CapeSlot,
                [EquipmentIndex.Body] = BodySlot,
                [EquipmentIndex.Gloves] = GlovesSlot,
                [EquipmentIndex.Leg] = LegSlot,
                [EquipmentIndex.Horse] = HorseSlot,
                [EquipmentIndex.HorseHarness] = HorseHarnessSlot,
            };
        }

        /// <summary>
        /// Lookup helper for slot view-models by equipment index.
        /// </summary>
        private EquipmentSlotVM GetSlotVm(EquipmentIndex index) =>
            _slotsByIndex.TryGetValue(index, out var vm) ? vm : null;

        /// <summary>
        /// Count the number of enabled equipment sets for the given battle type.
        /// </summary>
        private int CountEnabled(BattleType t)
        {
            var troop = State.Troop;
            if (troop == null)
                return 0;

            int c = 0;
            var eqs = troop.Loadout.Equipments;
            for (int i = 0; i < eqs.Count; i++)
            {
                var we = eqs[i];
                if (we.IsCivilian)
                    continue;
                if (CombatEquipmentBehavior.IsEnabled(troop, i, t))
                    c++;
            }
            return c;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Crafted ━━━━━━━ */

        [DataSourceProperty]
        public bool CanShowCrafted =>
            DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>()
            && EquipmentListVM.WeaponSlots.Contains(State.Slot.ToString())
            && !EditorVM.IsStudioMode;

        [DataSourceProperty]
        public bool ShowCrafted => EquipmentList.ShowCrafted && !EditorVM.IsStudioMode;

        [DataSourceProperty]
        public bool ShowCraftedIsVisible => IsVisible && !EditorVM.IsStudioMode;

        /* ━━━ Unequip / Unstage ━━ */

        [DataSourceProperty]
        public bool CanUnstage => EquipmentSlots.Any(s => s.IsStaged);

        [DataSourceProperty]
        public bool CanUnequip => State.Equipment?.Items.Any() == true;

        [DataSourceProperty]
        public string UnequipAllButtonText => L.S("unequip_all_button_text", "Unequip All");

        [DataSourceProperty]
        public string UnstageAllButtonText => L.S("unstage_all_button_text", "Reset Changes");

        /* ━━━━ Equipment Sets ━━━━ */

        [DataSourceProperty]
        public string EquipmentName => (State.Equipment?.Index + 1).ToString();

        [DataSourceProperty]
        public bool CanSelectPrevSet => (State.Equipment?.Index ?? 0) > 0;

        [DataSourceProperty]
        public bool CanSelectNextSet =>
            (State.Equipment?.Index ?? 0) < ((State.Troop?.Loadout.Equipments.Count ?? 1) - 1);

        [DataSourceProperty]
        public bool CanRemoveSet
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return false;

                var civs = troop.Loadout.GetCivilianSets().Count();
                var bats = troop.Loadout.GetBattleSets().Count();

                return eq.IsCivilian ? civs > 1 : bats > 1;
            }
        }

        [DataSourceProperty]
        public bool ShowSetControls => !ModCompatibility.NoAlternateEquipmentSets;

        [DataSourceProperty]
        public bool CanCreateSet => !ModCompatibility.NoAlternateEquipmentSets;

        [DataSourceProperty]
        public bool SetIsCivilian => State.Equipment?.IsCivilian == true;

        [DataSourceProperty]
        public bool SetIsBattle => State.Equipment?.IsCivilian == false;

        [DataSourceProperty]
        public bool CanToggleCivilianSet
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return false;

                // Target after click
                bool targetCivilian = !eq.IsCivilian;

                if (targetCivilian)
                {
                    if (eq.Index == 0)
                        return false; // First set cannot be civilian
                    // Making this battle set civilian must leave >=1 battle set
                    if (!eq.IsCivilian && troop.Loadout.GetBattleSets().Count() <= 1)
                        return false;
                }
                else
                {
                    // Making this civilian set battle must leave >=1 civilian set
                    if (eq.IsCivilian && troop.Loadout.GetCivilianSets().Count() <= 1)
                        return false;
                }

                return true;
            }
        }

        [DataSourceProperty]
        public bool SetIsEnabledForFieldBattle =>
            !SetIsCivilian
            && CombatEquipmentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                BattleType.FieldBattle
            );

        [DataSourceProperty]
        public bool SetIsEnabledForSiegeDefense =>
            !SetIsCivilian
            && CombatEquipmentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                BattleType.SiegeDefense
            );

        [DataSourceProperty]
        public bool SetIsEnabledForSiegeAssault =>
            !SetIsCivilian
            && CombatEquipmentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                BattleType.SiegeAssault
            );

        /// <summary>
        /// Handle slot selection changes and update the affected slot buttons.
        /// </summary>
        protected override void OnSlotChange()
        {
            EquipmentList.ShowCrafted = false; // reset crafted toggle on slot change

            var previous = _lastSlotIndex;
            var current = State.Slot;

            if (previous.HasValue && previous.Value != current)
                GetSlotVm(previous.Value)?.OnSlotChanged();

            GetSlotVm(current)?.OnSlotChanged();
            _lastSlotIndex = current;
        }

        /// <summary>
        /// Handle equipment loadout changes for the current slot button.
        /// </summary>
        protected override void OnEquipmentChange() => GetSlotVm(State.Slot)?.OnEquipmentChanged();

        /// <summary>
        /// Handle staged equip changes for the current slot button.
        /// </summary>
        protected override void OnEquipChange()
        {
            GetSlotVm(State.Slot)?.OnEquipChanged();

            if (State.Slot == EquipmentIndex.Horse)
            {
                GetSlotVm(EquipmentIndex.HorseHarness)?.OnEquipChanged();
            }
        }

        /* ━━━━━━━ Tooltips ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveSetHint =>
            CanRemoveSet
                ? null
                : Tooltip.MakeTooltip(
                    null,
                    L.T("remove_set_hint", "At least one set of this type must remain.").ToString()
                );

        [DataSourceProperty]
        public BasicTooltipViewModel CreateSetHint =>
            CanCreateSet
                ? null
                : Tooltip.MakeTooltip(
                    null,
                    L.T("create_set_hint", "Disabled due to conflicting mods (Shokuho).").ToString()
                );

        [DataSourceProperty]
        public BasicTooltipViewModel CivilianHint
        {
            get
            {
                if (!CanToggleCivilianSet)
                {
                    var eq = State.Equipment;
                    var msg =
                        (eq?.IsCivilian == true)
                            ? L.S(
                                "civilian_toggle_forbidden_battle_left",
                                "You must keep at least one civilian set."
                            )
                        : eq.Index != 0
                            ? L.S(
                                "civilian_toggle_forbidden_civilian_left",
                                "You must keep at least one battle set."
                            )
                        : L.S(
                            "civilian_toggle_forbidden_first_set",
                            "The first set must remain a battle set."
                        );
                    return Tooltip.MakeTooltip(null, msg);
                }

                return Tooltip.MakeTooltip(
                    null,
                    L.T("civilian_hint", "Enable or disable this set for civilian use.").ToString()
                );
            }
        }

        [DataSourceProperty]
        public bool CanToggleEnableForFieldBattle =>
            SetIsBattle
            && ( // only battle sets participate
                !SetIsEnabledForFieldBattle // enabling is always allowed
                || CountEnabled(BattleType.FieldBattle) > 1 // disabling allowed only if another remains
            );

        [DataSourceProperty]
        public bool CanToggleEnableForSiegeDefense =>
            SetIsBattle
            && (!SetIsEnabledForSiegeDefense || CountEnabled(BattleType.SiegeDefense) > 1);

        [DataSourceProperty]
        public bool CanToggleEnableForSiegeAssault =>
            SetIsBattle
            && (!SetIsEnabledForSiegeAssault || CountEnabled(BattleType.SiegeAssault) > 1);

        [DataSourceProperty]
        public BasicTooltipViewModel FieldBattleHint =>
            !SetIsBattle
                ? Tooltip.MakeTooltip(
                    null,
                    L.S("hint_set_disabled", "Can't enable for civilian sets.")
                )
            : (!CanToggleEnableForFieldBattle && SetIsEnabledForFieldBattle)
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "hint_last_enabled",
                        "At least one battle set must remain enabled for each battle type."
                    )
                )
            : Tooltip.MakeTooltip(null, L.S("hint_field_ok", "Available in field battles."));

        [DataSourceProperty]
        public BasicTooltipViewModel SiegeDefenseHint =>
            !SetIsBattle
                ? Tooltip.MakeTooltip(
                    null,
                    L.S("hint_set_disabled", "Can't enable for civilian sets.")
                )
            : (!CanToggleEnableForSiegeDefense && SetIsEnabledForSiegeDefense)
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "hint_last_enabled",
                        "At least one battle set must remain enabled for each battle type."
                    )
                )
            : Tooltip.MakeTooltip(null, L.S("hint_def_ok", "Available while defending a siege."));

        [DataSourceProperty]
        public BasicTooltipViewModel SiegeAssaultHint =>
            !SetIsBattle
                ? Tooltip.MakeTooltip(
                    null,
                    L.S("hint_set_disabled", "Can't enable for civilian sets.")
                )
            : (!CanToggleEnableForSiegeAssault && SetIsEnabledForSiegeAssault)
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "hint_last_enabled",
                        "At least one battle set must remain enabled for each battle type."
                    )
                )
            : Tooltip.MakeTooltip(
                null,
                L.S("hint_assault_ok", "Available while assaulting a siege.")
            );

        [DataSourceProperty]
        public BasicTooltipViewModel ShowCraftedHint =>
            CanShowCrafted
                ? Tooltip.MakeTooltip(
                    null,
                    ShowCrafted
                        ? L.S("hide_crafted_hint", "Hide crafted items.")
                        : L.S("show_crafted_hint", "Show crafted items.")
                )
            : !DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>()
                ? Tooltip.MakeTooltip(
                    null,
                    L.T(
                            "show_crafted_disabled_hint",
                            "Unlock the 'Clanic Traditions' doctrine to show crafted items."
                        )
                        .ToString()
                )
            : Tooltip.MakeTooltip(
                null,
                L.T("show_crafted_weapon_hint", "Only weapon slots can have crafted items.")
                    .ToString()
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteToggleShowCrafted()
        {
            if (!CanShowCrafted)
                return;
            EquipmentList.ShowCrafted = !EquipmentList.ShowCrafted;
            OnPropertyChanged(nameof(ShowCrafted));
            OnPropertyChanged(nameof(ShowCraftedHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleCivilianSet()
        {
            var troop = State.Troop;
            var eq = State.Equipment;
            if (troop == null || eq == null)
                return;
            if (!CanToggleCivilianSet)
                return;

            bool targetCivilian = !eq.IsCivilian;

            // Only need the warning when we're switching TO civilian (and not studio mode).
            if (targetCivilian)
            {
                var badSlots = new List<EquipmentIndex>();
                var badItems = new List<WItem>();

                // Collect non-civilian equipped items on this set
                foreach (var slot in WEquipment.Slots)
                {
                    var it = eq.Get(slot); // current equipped item on this slot
                    if (it != null && !it.IsCivilian) // non-civilian item found
                    {
                        badSlots.Add(slot);
                        badItems.Add(it);
                    }
                }

                if (badSlots.Count > 0)
                {
                    var title = L.S("make_civilian_title", "Make set civilian?");
                    var text = L.T(
                            "toggle_to_civilian_warn",
                            "This set has {COUNT} non-civilian item(s). They will be unequipped if you continue.\n\nProceed?"
                        )
                        .SetTextVariable("COUNT", badSlots.Count)
                        .ToString();

                    InformationManager.ShowInquiry(
                        new InquiryData(
                            title,
                            text,
                            true,
                            true,
                            L.S("confirm", "Confirm"),
                            L.S("cancel", "Cancel"),
                            affirmativeAction: () =>
                            {
                                // 1) Stock + unstage + unset each offending item
                                for (int i = 0; i < badSlots.Count; i++)
                                {
                                    var slot = badSlots[i];
                                    var it = eq.Get(slot);
                                    if (!EditorVM.IsStudioMode)
                                        it?.Stock(); // keep it available in stocks
                                    eq.UnstageItem(slot); // clear any pending staged change
                                    eq.UnsetItem(slot); // remove from the set
                                }

                                // 2) Flip to civilian (also enforces min-sets + normalize)
                                troop.Loadout.ToggleCivilian(eq, targetCivilian);

                                // 3) Ensure combat situation masks still have ≥1 enabled set
                                State.FixCombatPolicies(troop);

                                // 4) Selection might have shifted after Normalize()
                                var list = troop.Loadout.Equipments;
                                State.UpdateEquipment(
                                    list[Mathf.Clamp(eq.Index, 0, list.Count - 1)]
                                );
                            },
                            negativeAction: () => { }
                        )
                    );
                    return; // stop here; popup will handle the rest
                }
            }

            // No offending items or switching away from civilian -> flip directly
            troop.Loadout.ToggleCivilian(eq, targetCivilian);
            State.FixCombatPolicies(troop);

            var eqs = troop.Loadout.Equipments;
            State.UpdateEquipment(eqs[Mathf.Clamp(eq.Index, 0, eqs.Count - 1)]);
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForFieldBattle()
        {
            if (!CanToggleEnableForFieldBattle)
                return;
            CombatEquipmentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                BattleType.FieldBattle
            );
            OnPropertyChanged(nameof(SetIsEnabledForFieldBattle));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForSiegeDefense()
        {
            if (!CanToggleEnableForSiegeDefense)
                return;
            CombatEquipmentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                BattleType.SiegeDefense
            );
            OnPropertyChanged(nameof(SetIsEnabledForSiegeDefense));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForSiegeAssault()
        {
            if (!CanToggleEnableForSiegeAssault)
                return;
            CombatEquipmentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                BattleType.SiegeAssault
            );
            OnPropertyChanged(nameof(SetIsEnabledForSiegeAssault));
        }

        [DataSourceMethod]
        /// <summary>
        /// Unequip all items from the current equipment set (with confirmation).
        /// </summary>
        public void ExecuteUnequipAll()
        {
            if (CanUnequip == false)
                return; // No-op if cannot unequip

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unequip_all", "Unequip All"),
                    text: EditorVM.IsStudioMode
                        ? L.T("unequip_all_text_studio", "Unequip all items worn by {TROOP_NAME}?")
                            .SetTextVariable("TROOP_NAME", State.Troop.Name)
                            .ToString()
                        : L.T(
                                "unequip_all_text",
                                "Unequip all items worn by {TROOP_NAME}?\n\nThey will be stocked for later use."
                            )
                            .SetTextVariable("TROOP_NAME", State.Troop.Name)
                            .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        State.Troop.UnequipAll(State.Equipment.Index, stock: true);
                        State.UpdateEquipment(State.Equipment);
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        /// <summary>
        /// Revert all staged equipment changes for the current equipment set (with confirmation).
        /// </summary>
        public void ExecuteUnstageAll()
        {
            if (CanUnstage == false)
                return; // No-op if nothing to unstage

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unstage_all", "Unstage All"),
                    text: L.T(
                            "unstage_all_text",
                            "Revert all staged equipment changes for {TROOP_NAME}?"
                        )
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        State.Troop.UnstageAll(State.Equipment.Index, true);
                        State.UpdateEquipment(State.Equipment);
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        /// <summary>
        /// Select the previous equipment set.
        /// </summary>
        public void ExecutePrevSet() =>
            State.UpdateEquipment(State.Troop.Loadout.Equipments[State.Equipment.Index - 1]);

        [DataSourceMethod]
        /// <summary>
        /// Select the next equipment set.
        /// </summary>
        public void ExecuteNextSet() =>
            State.UpdateEquipment(State.Troop.Loadout.Equipments[State.Equipment.Index + 1]);

        [DataSourceMethod]
        /// <summary>
        /// Remove the current alternate equipment set (with confirmation).
        /// </summary>
        public void ExecuteRemoveSet()
        {
            if (CanRemoveSet == false)
                return;

            InformationManager.ShowInquiry(
                new InquiryData(
                    L.S("remove_set_title", "Remove Set"),
                    EditorVM.IsStudioMode
                        ? L.T("remove_set_text_studio", "Remove set n°{EQUIPMENT} for {TROOP}?")
                            .SetTextVariable("EQUIPMENT", EquipmentName)
                            .SetTextVariable("TROOP", State.Troop.Name)
                            .ToString()
                        : L.T(
                                "remove_set_text",
                                "Remove set n°{EQUIPMENT} for {TROOP}?\nAll staged changes will be cleared and items will be unequipped and stocked."
                            )
                            .SetTextVariable("EQUIPMENT", EquipmentName)
                            .SetTextVariable("TROOP", State.Troop.Name)
                            .ToString(),
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Clear staged & unequip first
                        State.Troop.UnequipAll(
                            State.Equipment?.Index ?? 0,
                            stock: !EditorVM.IsStudioMode
                        );
                        State.Troop.UnstageAll(State.Equipment?.Index ?? 0, !EditorVM.IsStudioMode);

                        // Persist mask removal and remove the set
                        CombatEquipmentBehavior.OnRemoved(State.Troop, State.Equipment.Index);
                        State.Troop.Loadout.Remove(State.Equipment);

                        // Keep masks valid and select canonical battle set (index 0)
                        State.FixCombatPolicies(State.Troop);
                        State.UpdateEquipment(State.Troop.Loadout.Battle);
                    },
                    () => { }
                )
            );
        }

        [DataSourceMethod]
        /// <summary>
        /// Create a new alternate equipment set:
        /// - Prompt: Copy current set OR create empty.
        /// - If copying and Config.PayForEquipment: compute and display total cost for missing stocks,
        ///   check affordability, pay and top-up stocks, then consume 1 stock per item copied.
        /// - New set starts disabled in combat masks; policies normalized; select created set.
        /// </summary>
        public void ExecuteCreateSet()
        {
            if (!CanCreateSet)
                return;

            var troop = State.Troop;
            var src = State.Equipment; // current set
            if (troop == null || src == null)
                return;

            // Compose the inquiry text (with or without cost hint, computed below when needed).
            var title = L.S("create_set_title", "Create Equipment Set");
            var baseText = L.S(
                "create_set_text",
                "Do you want to copy the current set or create an empty set?"
            );

            InformationManager.ShowInquiry(
                new InquiryData(
                    title,
                    baseText,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("copy_current", "Copy Current"),
                    negativeText: L.S("empty_set", "Empty"),
                    affirmativeAction: () => ExecuteCreateSet_CopyFlow(troop, src),
                    negativeAction: () => ExecuteCreateSet_EmptyFlow(troop)
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Create an empty battle set, then apply mask defaults and select it.
        /// </summary>
        private void ExecuteCreateSet_EmptyFlow(WCharacter troop)
        {
            // New default = create a BATTLE set
            var created = troop?.Loadout.CreateBattle();
            if (created == null)
                return;

            // Default to disabled for all battle types, then fix invariants
            CombatEquipmentBehavior.DisableAll(troop, created.Index);
            State.FixCombatPolicies(troop);

            // Select the newly created set
            State.UpdateEquipment(created);
        }

        /// <summary>
        /// Copy the current set into a new one. If PayForEquipment=false or StudioMode:
        ///  - direct copy of items (no payment, no stock checks).
        /// Else:
        ///  - compute missing-stock items, sum their costs, ensure affordability,
        ///  - charge gold & top-up stock by +1 for each missing item,
        ///  - create the new set, equip all items into it,
        ///  - consume 1 stock per equipped item.
        /// </summary>
        private void ExecuteCreateSet_CopyFlow(WCharacter troop, WEquipment src)
        {
            // Studio mode or free equipment: do a straight copy (no cost, no stocks)
            bool free = EditorVM.IsStudioMode || Config.PayForEquipment == false;

            // Build the per-item copy plan from the source set
            var plan = CollectCopyPlan(src);

            if (free)
            {
                var created = troop.Loadout.CreateBattle();
                CopyItemsInto(created, plan);
                CombatEquipmentBehavior.DisableAll(troop, created.Index);
                State.FixCombatPolicies(troop);
                State.UpdateEquipment(created);
                return;
            }

            // Paid path: precompute total gold required for items that are NOT stocked
            int totalCost = 0;
            foreach (var (slot, item, isStocked) in plan)
            {
                if (!isStocked)
                    totalCost += EquipmentManager.GetItemCost(item, troop);
            }

            // If there's a cost, announce it and ask for confirmation (and affordability)
            if (totalCost > 0)
            {
                // If the player can't afford, show a popup and abort
                if (Player.Gold < totalCost)
                {
                    Notifications.Popup(
                        L.T("not_enough_gold_title", "Not Enough Gold"),
                        L.T(
                                "set_not_enough_gold_text",
                                "You need {COST} gold to purchase missing items."
                            )
                            .SetTextVariable("COST", Format.Number(totalCost))
                    );
                    return;
                }

                // Ask confirmation with the total cost
                var title = L.S("confirm_copy_cost_title", "Confirm Purchase");
                var text = L.T(
                        "confirm_copy_cost_text",
                        "Copying this set requires purchasing missing items for {COST} gold. Proceed?"
                    )
                    .SetTextVariable("COST", Format.Number(totalCost))
                    .ToString();

                InformationManager.ShowInquiry(
                    new InquiryData(
                        title,
                        text,
                        true,
                        true,
                        L.S("confirm", "Confirm"),
                        L.S("cancel", "Cancel"),
                        affirmativeAction: () =>
                            ExecuteCreateSet_CopyPaid_Do(troop, src, plan, totalCost),
                        negativeAction: () => { }
                    )
                );
            }
            else
            {
                // No cost (everything already stocked)
                ExecuteCreateSet_CopyPaid_Do(troop, src, plan, totalCost);
            }
        }

        /// <summary>
        /// Finalize the paid copy: charge, top-up missing stocks, create set, equip items, then consume stocks.
        /// </summary>
        private void ExecuteCreateSet_CopyPaid_Do(
            WCharacter troop,
            WEquipment src,
            List<(EquipmentIndex slot, WItem item, bool isStocked)> plan,
            int totalCost
        )
        {
            try
            {
                // 1) Pay once for all missing-stock items and top-up their stock by +1
                foreach (var (slot, item, isStocked) in plan)
                {
                    if (!isStocked)
                    {
                        // Pay (uses doctrine rebates via EquipmentManager)
                        var cost = EquipmentManager.GetItemCost(item, troop);
                        if (cost > 0)
                            Player.ChangeGold(-cost);

                        // Top-up
                        item.Stock();
                    }
                }

                // 2) Create the new battle set
                var created = troop.Loadout.CreateBattle();

                // 3) Equip: set items directly into the created set
                CopyItemsInto(created, plan);

                // 4) Consume 1 stock for each equipped item
                foreach (var (slot, item, isStocked) in plan)
                    item.Unstock();

                // 5) Default mask state and policy fix
                CombatEquipmentBehavior.DisableAll(troop, created.Index);
                State.FixCombatPolicies(troop);

                // 6) Select the new set
                State.UpdateEquipment(created);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                Notifications.Popup(
                    L.T("copy_failed_title", "Copy Failed"),
                    L.T("copy_failed_text", "Something went wrong while copying this set.")
                );
            }
        }

        /// <summary>
        /// Build (slot, item, isStocked) for all defined slots from the source equipment.
        /// </summary>
        private List<(EquipmentIndex slot, WItem item, bool isStocked)> CollectCopyPlan(
            WEquipment src
        )
        {
            var list = new List<(EquipmentIndex, WItem, bool)>(WEquipment.Slots.Count);
            foreach (var slot in WEquipment.Slots)
            {
                var it = src.Get(slot);
                if (it == null)
                    continue;

                // If your API is StockCount > 0, replace with (it.StockCount > 0)
                bool stocked = false;
                try
                {
                    stocked = it.IsStocked; // adjust if your wrapper uses a different name
                }
                catch
                { /* fallback if property not present; treat as not stocked */
                }

                list.Add((slot, it, stocked));
            }
            return list;
        }

        /// <summary>
        /// Copy items (by slot) into the destination equipment (direct set, no timing/staging).
        /// </summary>
        private static void CopyItemsInto(
            WEquipment dst,
            List<(EquipmentIndex slot, WItem item, bool isStocked)> plan
        )
        {
            foreach (var (slot, item, _) in plan)
                dst.SetItem(slot, item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Show the equipment screen and its child components.
        /// </summary>
        public override void Show()
        {
            base.Show();
            EquipmentList.Show();
            foreach (var slot in EquipmentSlots)
                slot.Show();

            // Ensure filter is refreshed when showing
            EquipmentList.RefreshFilter();
        }

        /// <summary>
        /// Hide the equipment screen and its child components.
        /// </summary>
        public override void Hide()
        {
            foreach (var slot in EquipmentSlots)
                slot.Hide();
            EquipmentList.Hide();
            base.Hide();
        }
    }
}
