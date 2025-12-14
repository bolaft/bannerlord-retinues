using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Agents;
using Retinues.Features.Staging;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.List;
using Retinues.GUI.Editor.VM.Equipment.Panel;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace OldRetinues.GUI.Editor.VM.Equipment
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
                    nameof(CopyEquipmentHint),
                    nameof(PasteEquipmentHint),
                    nameof(CopyEquipmentIconColor),
                    nameof(PasteEquipmentIconColor),
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
                    nameof(GenderOverrideIcon),
                    nameof(SetIsCivilian),
                    nameof(SetIsBattle),
                    nameof(SetIsEnabledForFieldBattle),
                    nameof(SetIsEnabledForNavalBattle),
                    nameof(SetIsEnabledForSiegeDefense),
                    nameof(SetIsEnabledForSiegeAssault),
                    nameof(SetHasGenderOverride),
                    nameof(FieldBattleHint),
                    nameof(NavalBattleHint),
                    nameof(SiegeDefenseHint),
                    nameof(SiegeAssaultHint),
                    nameof(SiegeHint),
                    nameof(GenderOverrideHint),
                    nameof(PreviewModeHint),
                    nameof(CivilianHint),
                    nameof(CanToggleEnableForFieldBattle),
                    nameof(CanToggleEnableForNavalBattle),
                    nameof(CanToggleEnableForSiegeDefense),
                    nameof(CanToggleEnableForSiegeAssault),
                    nameof(CopyEquipmentHint),
                    nameof(PasteEquipmentHint),
                    nameof(CopyEquipmentIconColor),
                    nameof(PasteEquipmentIconColor),
                ],
                [UIEvent.Equip] = [nameof(CanUnstage), nameof(CanUnequip)],
                [UIEvent.Slot] =
                [
                    nameof(CanUnstage),
                    nameof(CanShowCrafted),
                    nameof(ShowCrafted),
                    nameof(ShowCraftedHint),
                ],
                [UIEvent.Appearance] = [nameof(GenderOverrideIcon)],
            };

        protected override void OnTroopChange()
        {
            // Disable preview mode on troop change
            PreviewOverlay.Disable();
            OnPropertyChanged(nameof(InPreviewMode));
            OnPropertyChanged(nameof(PreviewModeHint));

            // Reset mode & selection for new troop
            _editCivilianSets = false;
            OnPropertyChanged(nameof(EditCivilianSets));
            EnsureValidSetForCurrentMode();
        }

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
        private int CountEnabled(PolicyToggleType t)
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
                if (CombatAgentBehavior.IsEnabled(troop, i, t))
                    c++;
            }
            return c;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ Set Category Mode ━━━━ */

        private bool _editCivilianSets = false;

        /// <summary>
        /// If true, prev/next/create/delete operate on civilian sets; otherwise battle sets.
        /// </summary>
        [DataSourceProperty]
        public bool EditCivilianSets
        {
            get => _editCivilianSets;
            set
            {
                if (_editCivilianSets == value)
                    return;

                _editCivilianSets = value;
                OnPropertyChanged(nameof(EditCivilianSets));
                OnPropertyChanged(nameof(CivilianHint));

                // Selection & bindings depend on mode
                EnsureValidSetForCurrentMode();

                OnPropertyChanged(nameof(EquipmentName));
                OnPropertyChanged(nameof(CanSelectPrevSet));
                OnPropertyChanged(nameof(CanSelectNextSet));
                OnPropertyChanged(nameof(CanCreateSet));
                OnPropertyChanged(nameof(CanRemoveSet));
            }
        }

        /// <summary>
        /// Ensure the currently selected equipment belongs to the chosen category.
        /// If not, select the canonical set of that category (if any).
        /// </summary>
        private void EnsureValidSetForCurrentMode()
        {
            var troop = State.Troop;
            if (troop == null)
                return;

            var current = State.Equipment;

            // No current selection → pick canonical set for this mode.
            if (current == null)
            {
                var canonical = _editCivilianSets
                    ? troop.Loadout.Civilian // hero: index 1; troop: first civilian
                    : troop.Loadout.Battle; // hero or troop battle set

                if (canonical != null)
                    State.UpdateEquipment(canonical);

                return;
            }

            // Wrong category for the chosen mode → switch to the canonical set of that mode.
            if (current.IsCivilian != _editCivilianSets)
            {
                var canonical = _editCivilianSets ? troop.Loadout.Civilian : troop.Loadout.Battle;

                if (canonical != null)
                    State.UpdateEquipment(canonical);

                return;
            }

            // Right category: keep it as long as its underlying Equipment still exists.
            var list = _editCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;
            if (!list.Any(e => ReferenceEquals(e.Base, current.Base)))
            {
                var first = list.FirstOrDefault();
                if (first != null)
                    State.UpdateEquipment(first);
            }
        }

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowEquipmentCheckboxes => IsVisible && !ClanScreen.IsStudioMode;

        [DataSourceProperty]
        public bool HasNavalDLC => ModCompatibility.HasNavalDLC;

        /* ━━━━━━━━ Crafted ━━━━━━━ */

        [DataSourceProperty]
        public bool CanShowCrafted =>
            (DoctrineAPI.IsDoctrineUnlocked<ClanicTraditions>() || ClanScreen.IsStudioMode)
            && EquipmentListVM.WeaponSlots.Contains(State.Slot.ToString());

        [DataSourceProperty]
        public bool ShowCrafted => EquipmentList.ShowCrafted;

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
        public string EquipmentName
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return "–";

                var list = EditCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;

                // Match by underlying Equipment instance, not wrapper reference.
                var localIndex = list.FindIndex(e => ReferenceEquals(e.Base, eq.Base));
                if (localIndex < 0)
                    return (eq.Index + 1).ToString(); // conservative fallback

                return (localIndex + 1).ToString();
            }
        }

        [DataSourceProperty]
        public bool CanSelectPrevSet
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return false;

                var list = EditCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;
                var idx = list.FindIndex(e => ReferenceEquals(e.Base, eq.Base));
                return idx > 0;
            }
        }

        [DataSourceProperty]
        public bool CanSelectNextSet
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return false;

                var list = EditCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;
                var idx = list.FindIndex(e => ReferenceEquals(e.Base, eq.Base));
                return idx >= 0 && idx < list.Count - 1;
            }
        }

        [DataSourceProperty]
        public bool ShowSetControls => true; // used to hide controls with Shokuho

        [DataSourceProperty]
        public bool CanCreateSet => !State.Troop.IsHero;

        [DataSourceProperty]
        public bool CanRemoveSet
        {
            get
            {
                var troop = State.Troop;
                var eq = State.Equipment;
                if (troop == null || eq == null)
                    return false;

                if (troop.IsHero)
                    return false; // Heroes cannot create/remove sets

                var civs = troop.Loadout.CivilianSets.Count();
                var bats = troop.Loadout.BattleSets.Count();

                return eq.IsCivilian ? civs > 1 : bats > 1;
            }
        }

        [DataSourceProperty]
        public bool SetIsCivilian => State.Equipment?.IsCivilian == true;

        [DataSourceProperty]
        public bool SetIsBattle => State.Equipment?.IsCivilian == false;

        [DataSourceProperty]
        public bool SetIsEnabledForFieldBattle =>
            !SetIsCivilian
            && CombatAgentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.FieldBattle
            );

        [DataSourceProperty]
        public bool SetIsEnabledForSiegeDefense =>
            !SetIsCivilian
            && CombatAgentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeDefense
            );

        [DataSourceProperty]
        public bool SetIsEnabledForSiegeAssault =>
            !SetIsCivilian
            && CombatAgentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeAssault
            );

        [DataSourceProperty]
        public bool SetIsEnabledForNavalBattle =>
            !SetIsCivilian
            && CombatAgentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.NavalBattle
            );

        [DataSourceProperty]
        public bool SetHasGenderOverride =>
            CombatAgentBehavior.IsEnabled(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.GenderOverride
            );

        [DataSourceProperty]
        public string GenderOverrideIcon =>
            State.Troop?.IsFemale == true
                ? "SPGeneral\\GeneralFlagIcons\\male_only"
                : "SPGeneral\\GeneralFlagIcons\\female_only";

        [DataSourceProperty]
        public bool InPreviewMode => PreviewOverlay.IsEnabled;

        [DataSourceProperty]
        public string PreviewModeIcon => "Inventory\\icon_inspect";

        [DataSourceProperty]
        public string PreviewModeText => L.S("preview_mode_text", "Preview Mode");

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
            CanRemoveSet ? null
            : State.Troop.IsHero
                ? Tooltip.MakeTooltip(
                    null,
                    L.T("remove_set_hero_hint", "Cannot remove equipment sets for heroes.")
                        .ToString()
                )
            : Tooltip.MakeTooltip(
                null,
                L.T("remove_set_hint", "At least one set of this type must remain.").ToString()
            );

        [DataSourceProperty]
        public BasicTooltipViewModel CreateSetHint =>
            CanCreateSet ? null
            : State.Troop.IsHero
                ? Tooltip.MakeTooltip(
                    null,
                    L.T("create_set_hero_hint", "Cannot create equipment sets for heroes.")
                        .ToString()
                )
            : Tooltip.MakeTooltip(
                null,
                L.T("create_set_hint", "Disabled due to conflicting mods (Shokuho).").ToString()
            );

        [DataSourceProperty]
        public BasicTooltipViewModel CivilianHint =>
            Tooltip.MakeTooltip(
                null,
                EditCivilianSets
                    ? L.S("civilian_set_enabled_hint", "Uncheck this box to switch to battle sets.")
                    : L.S(
                        "civilian_set_disabled_hint",
                        "Check this box to switch to civilian sets."
                    )
            );

        [DataSourceProperty]
        public bool CanToggleEnableForFieldBattle =>
            SetIsBattle
            && ( // only battle sets participate
                !SetIsEnabledForFieldBattle // enabling is always allowed
                || CountEnabled(PolicyToggleType.FieldBattle) > 1 // disabling allowed only if another remains
            );

        [DataSourceProperty]
        public bool CanToggleEnableForSiegeDefense =>
            SetIsBattle
            && (!SetIsEnabledForSiegeDefense || CountEnabled(PolicyToggleType.SiegeDefense) > 1);

        [DataSourceProperty]
        public bool CanToggleEnableForSiegeAssault =>
            SetIsBattle
            && (!SetIsEnabledForSiegeAssault || CountEnabled(PolicyToggleType.SiegeAssault) > 1);

        [DataSourceProperty]
        public bool CanToggleEnableForNavalBattle =>
            !SetIsCivilian
            && (!SetIsEnabledForNavalBattle || CountEnabled(PolicyToggleType.NavalBattle) > 1);

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
            : Tooltip.MakeTooltip(null, L.S("hint_field_ok", "Enable for field battles."));

        [DataSourceProperty]
        public BasicTooltipViewModel NavalBattleHint =>
            !SetIsBattle
                ? Tooltip.MakeTooltip(
                    null,
                    L.S("hint_set_disabled", "Can't enable for civilian sets.")
                )
            : (!CanToggleEnableForNavalBattle && SetIsEnabledForNavalBattle)
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "hint_last_enabled",
                        "At least one battle set must remain enabled for each battle type."
                    )
                )
            : Tooltip.MakeTooltip(null, L.S("hint_naval_ok", "Enable for naval battles."));

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
            : Tooltip.MakeTooltip(null, L.S("hint_def_ok", "Enable for siege defense."));

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
            : Tooltip.MakeTooltip(null, L.S("hint_assault_ok", "Enable for siege assault."));

        [DataSourceProperty]
        public BasicTooltipViewModel SiegeHint =>
            !SetIsBattle
                ? Tooltip.MakeTooltip(
                    null,
                    L.S("hint_set_disabled", "Can't enable for civilian sets.")
                )
            : (
                (!CanToggleEnableForSiegeAssault && SetIsEnabledForSiegeAssault)
                || (!CanToggleEnableForSiegeDefense && SetIsEnabledForSiegeDefense)
            )
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "hint_last_enabled",
                        "At least one battle set must remain enabled for each battle type."
                    )
                )
            : Tooltip.MakeTooltip(null, L.S("hint_siege_ok", "Enable for siege battles."));

        [DataSourceProperty]
        public BasicTooltipViewModel GenderOverrideHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "gender_override_hint",
                    "If enabled, troops spawning with this equipment set will be of the opposite gender."
                )
            );

        [DataSourceProperty]
        public BasicTooltipViewModel PreviewModeHint =>
            Tooltip.MakeTooltip(
                null,
                InPreviewMode
                    ? L.S("preview_mode_disable_hint", "Disable Preview Mode.")
                    : L.S(
                        "preview_mode_enable_hint",
                        "Preview Mode: see how equipment looks on the troop without applying changes."
                    )
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
                    L.S(
                        "show_crafted_disabled_hint",
                        "Unlock the 'Clan Traditions' doctrine to show crafted items."
                    )
                )
            : Tooltip.MakeTooltip(
                null,
                L.S("show_crafted_weapon_hint", "Only weapon slots can have crafted items.")
            );

        [DataSourceProperty]
        public BasicTooltipViewModel CopyEquipmentHint
        {
            get
            {
                if (State.Equipment == null)
                {
                    return Tooltip.MakeTooltip(
                        null,
                        L.S("copy_equipment_disabled", "No equipment set selected.")
                    );
                }

                return Tooltip.MakeTooltip(
                    null,
                    L.S("copy_equipment_hint", "Copy this equipment set to the clipboard.")
                );
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel PasteEquipmentHint
        {
            get
            {
                if (Clipboard == null)
                {
                    return Tooltip.MakeTooltip(
                        null,
                        L.S(
                            "paste_equipment_empty",
                            "Clipboard is empty. Copy an equipment set first."
                        )
                    );
                }

                return Tooltip.MakeTooltip(
                    null,
                    L.S("paste_equipment_hint", "Paste the copied equipment onto this set.")
                );
            }
        }

        /* ━━━━━━━━ Colors ━━━━━━━━ */

        const string HoveredColor = "#fdae1ae8";
        const string EnabledColor = "#f8d28ab4";
        const string DisabledColor = "#46433db4";

        private bool _copyIsHovered = false;
        private bool _pasteIsHovered = false;

        [DataSourceProperty]
        public string CopyEquipmentIconColor => _copyIsHovered ? HoveredColor : EnabledColor;

        [DataSourceProperty]
        public string PasteEquipmentIconColor
        {
            get
            {
                if (Clipboard != null)
                    return _pasteIsHovered ? HoveredColor : EnabledColor;

                return DisabledColor;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Hover ━━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteBeginHoverCopyIcon()
        {
            _copyIsHovered = true;
            OnPropertyChanged(nameof(CopyEquipmentIconColor));
        }

        [DataSourceMethod]
        public void ExecuteEndHoverCopyIcon()
        {
            _copyIsHovered = false;
            OnPropertyChanged(nameof(CopyEquipmentIconColor));
        }

        [DataSourceMethod]
        public void ExecuteBeginHoverPasteIcon()
        {
            _pasteIsHovered = true;
            OnPropertyChanged(nameof(PasteEquipmentIconColor));
        }

        [DataSourceMethod]
        public void ExecuteEndHoverPasteIcon()
        {
            _pasteIsHovered = false;
            OnPropertyChanged(nameof(PasteEquipmentIconColor));
        }

        /* ━━━━ Equipment Sets ━━━━ */

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
        public void ExecuteToggleEnableSetForFieldBattle()
        {
            if (!CanToggleEnableForFieldBattle)
                return;
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.FieldBattle
            );
            OnPropertyChanged(nameof(SetIsEnabledForFieldBattle));
            OnPropertyChanged(nameof(FieldBattleHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForNavalBattle()
        {
            if (!CanToggleEnableForNavalBattle)
                return;
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.NavalBattle
            );
            OnPropertyChanged(nameof(SetIsEnabledForNavalBattle));
            OnPropertyChanged(nameof(NavalBattleHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForSiegeDefense()
        {
            if (!CanToggleEnableForSiegeDefense)
                return;
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeDefense
            );
            OnPropertyChanged(nameof(SetIsEnabledForSiegeDefense));
            OnPropertyChanged(nameof(SiegeDefenseHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForSiegeAssault()
        {
            if (!CanToggleEnableForSiegeAssault)
                return;
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeAssault
            );
            OnPropertyChanged(nameof(SetIsEnabledForSiegeAssault));
            OnPropertyChanged(nameof(SiegeAssaultHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleEnableSetForSiege()
        {
            if (!CanToggleEnableForSiegeAssault && !CanToggleEnableForSiegeDefense)
                return;
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeAssault
            );
            CombatAgentBehavior.Toggle(
                State.Troop,
                State.Equipment.Index,
                PolicyToggleType.SiegeDefense
            );
            OnPropertyChanged(nameof(SetIsEnabledForSiegeAssault));
            OnPropertyChanged(nameof(SetIsEnabledForSiegeDefense));
            OnPropertyChanged(nameof(SiegeAssaultHint));
            OnPropertyChanged(nameof(SiegeDefenseHint));
        }

        [DataSourceMethod]
        public void ExecuteToggleGenderOverride()
        {
            var troop = State.Troop;
            var eq = State.Equipment;
            if (troop == null || eq == null)
                return;

            CombatAgentBehavior.Toggle(troop, eq.Index, PolicyToggleType.GenderOverride);
            OnPropertyChanged(nameof(SetHasGenderOverride));
            OnPropertyChanged(nameof(GenderOverrideHint));

            // Update 3D model + icon when the override changes.
            State.UpdateAppearance();
        }

        [DataSourceMethod]
        public void ExecuteTogglePreviewMode()
        {
            PreviewOverlay.Toggle();
            State.UpdateEquipment(State.Equipment); // Trigger rows refresh
            OnPropertyChanged(nameof(InPreviewMode));
            OnPropertyChanged(nameof(PreviewModeHint));
        }

        [DataSourceMethod]
        public void ExecuteUnequipAll()
        {
            if (!CanUnequip)
                return;

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unequip_all", "Unequip All"),
                    text: L.T("unequip_all_text", "Unequip all items worn by {TROOP_NAME}?")
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        var troop = State.Troop;
                        var setIndex = State.Equipment.Index;

                        foreach (var slot in WEquipment.Slots)
                        {
                            // 1. If a staged change exists, roll it back
                            var pending = EquipStagingBehavior.Get(troop, slot, setIndex);
                            if (pending != null)
                            {
                                var stagedItem = new WItem(pending.ItemId);
                                EquipmentManager.RollbackStagedEquip(
                                    troop,
                                    setIndex,
                                    slot,
                                    stagedItem
                                );
                            }

                            // 2. Unequip (ignore individual warnings, user already confirmed)
                            EquipmentManager.TryUnequip(troop, setIndex, slot);
                        }

                        State.UpdateEquipment(State.Equipment);
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteUnstageAll()
        {
            if (!CanUnstage)
                return;

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("unstage_all", "Reset Changes"),
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
                        var troop = State.Troop;
                        var setIndex = State.Equipment.Index;

                        // For every slot, if staged → rollback and unstage
                        foreach (var slot in WEquipment.Slots)
                        {
                            var pending = EquipStagingBehavior.Get(troop, slot, setIndex);
                            if (pending == null)
                                continue;

                            var stagedItem = new WItem(pending.ItemId);

                            // Rollback staged equip
                            EquipmentManager.RollbackStagedEquip(troop, setIndex, slot, stagedItem);
                        }

                        State.UpdateEquipment(State.Equipment);
                    },
                    negativeAction: () => { }
                )
            );
        }

        /// <summary>
        /// Copy the current equipment set to the clipboard.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteCopyEquipment()
        {
            if (State.Equipment == null)
                return;

            Clipboard = State.Equipment;

            OnPropertyChanged(nameof(CopyEquipmentHint));
            OnPropertyChanged(nameof(PasteEquipmentHint));
            OnPropertyChanged(nameof(CopyEquipmentIconColor));
            OnPropertyChanged(nameof(PasteEquipmentIconColor));

            Notifications.Log(L.S("equipment_copied", "Equipment set copied to clipboard."));
        }

        /// <summary>
        /// Paste the clipboard equipment onto the current set.
        /// </summary>
        [DataSourceMethod]
        public void ExecutePasteEquipment()
        {
            // nothing to paste
            if (Clipboard == null)
                return;

            var source = Clipboard;
            var target = State.Equipment;
            if (target == null)
                return;

            var troop = target.Loadout.Troop;
            if (troop == null)
                return;

            // Build unlock mask: which slots from the clipboard are actually unlocked
            // for this troop in the current (personal/global) context.
            var allowedSlots = new HashSet<EquipmentIndex>();
            var lockedSlots = new List<EquipmentIndex>();

            foreach (var slot in WEquipment.Slots)
            {
                var srcItem = source.Get(slot);
                if (srcItem == null)
                    continue;

                if (EquipmentManager.IsUnlockedForPaste(troop, slot, srcItem))
                    allowedSlots.Add(slot);
                else
                    lockedSlots.Add(slot);
            }

            // Nothing from this set is unlocked → nothing meaningful to paste.
            if (allowedSlots.Count == 0)
            {
                Notifications.Popup(
                    L.T("paste_equip_all_locked_title", "Nothing Unlocked"),
                    L.T(
                        "paste_equip_all_locked_text",
                        "No items from this equipment set are currently unlocked for this troop."
                    )
                );
                return;
            }

            // Only restrict slots when some items are locked; otherwise let the manager
            // run as before (null = all slots).
            HashSet<EquipmentIndex> allowedSlotsOrNull =
                lockedSlots.Count > 0 ? allowedSlots : null;

            var cost = EquipmentManager.QuotePasteGoldCost(source, target, allowedSlotsOrNull);
            var gold = Player.Gold;

            // cannot afford
            if (cost > 0 && gold < cost)
            {
                Notifications.Popup(
                    L.T("paste_equip_cannot_afford_title", "Cannot Afford"),
                    L.T(
                            "paste_equip_cannot_afford_text",
                            "You need {GOLD} gold to paste this equipment, but you only have {CURRENT}."
                        )
                        .SetTextVariable("GOLD", cost)
                        .SetTextVariable("CURRENT", gold)
                );
                return;
            }

            var troopName = source.Loadout.Troop.Name;
            var equipmentNumber = (source.Index + 1).ToString();
            var hasLockedItems = lockedSlots.Count > 0;

            // confirmation
            var title = hasLockedItems
                ? L.T("paste_equip_locked_confirm_title", "Confirm Partial Equipment Copy")
                : L.T("paste_equip_confirm_title", "Confirm Equipment Copy");

            TextObject text;

            if (hasLockedItems)
            {
                text =
                    cost > 0
                        ? L.T(
                                "paste_equip_locked_confirm_text_cost",
                                "Some items in {TROOP}'s equipment n°{INDEX} are still locked and will be skipped. Pasting the unlocked items will cost {GOLD} gold. Continue?"
                            )
                            .SetTextVariable("GOLD", cost)
                            .SetTextVariable("TROOP", troopName)
                            .SetTextVariable("INDEX", equipmentNumber)
                        : L.T(
                                "paste_equip_locked_confirm_text_free",
                                "Some items in {TROOP}'s equipment n°{INDEX} are still locked and will be skipped. Paste the unlocked items onto the current set?"
                            )
                            .SetTextVariable("TROOP", troopName)
                            .SetTextVariable("INDEX", equipmentNumber);
            }
            else
            {
                text =
                    cost > 0
                        ? L.T(
                                "paste_equip_confirm_text_cost",
                                "Pasting {TROOP}'s equipment n°{INDEX} will cost {GOLD} gold. Continue?"
                            )
                            .SetTextVariable("GOLD", cost)
                            .SetTextVariable("TROOP", troopName)
                            .SetTextVariable("INDEX", equipmentNumber)
                        : L.T(
                                "paste_equip_confirm_text_free",
                                "Paste {TROOP}'s equipment n°{INDEX} onto the current set?"
                            )
                            .SetTextVariable("TROOP", troopName)
                            .SetTextVariable("INDEX", equipmentNumber);
            }

            Notifications.ConfirmationPopup(
                title,
                text,
                onConfirm: () =>
                {
                    var res = EquipmentManager.TryPasteEquipment(
                        source,
                        target,
                        allowedSlotsOrNull
                    );

                    if (!res.Ok)
                    {
                        string msg = res.Reason switch
                        {
                            EquipmentManager.EquipFailReason.NotAllowed =>
                                BuildPasteNotAllowedMessage(res),
                            EquipmentManager.EquipFailReason.NotEnoughGold => L.S(
                                "paste_failed_not_enough_gold",
                                "You do not have enough gold."
                            ),
                            EquipmentManager.EquipFailReason.NotEnoughStock => L.S(
                                "paste_failed_not_enough_stock",
                                "You lack enough copies of required items."
                            ),
                            EquipmentManager.EquipFailReason.NotCivilian => L.S(
                                "paste_failed_not_civilian",
                                "A non-civilian item cannot be equipped in a civilian set."
                            ),
                            _ => L.S("paste_failed_generic", "The equipment paste failed."),
                        };
                        Notifications.Popup(
                            L.T("paste_equip_failed_title", "Copy Failed"),
                            L.T("paste_equip_failed_text", msg)
                        );
                        return;
                    }

                    // success
                    Clipboard = null;

                    State.UpdateEquipment(State.Equipment);
                }
            );
        }

        private static string BuildPasteNotAllowedMessage(EquipmentManager.PasteResult res)
        {
            var reasons = res.Details;

            // Fallback: old generic message if we have no more detail.
            if (reasons == EquipmentManager.EquipLimitReason.None)
            {
                return L.S(
                    "paste_failed_not_allowed",
                    "Some items cannot be equipped by this troop."
                );
            }

            var lines = new List<string>();

            if (reasons.HasFlag(EquipmentManager.EquipLimitReason.MountT1))
            {
                lines.Add(
                    "• "
                        + L.S(
                            "paste_failed_reason_mount_t1",
                            "Tier 1 troops are not allowed to have a mount."
                        )
                );
            }

            if (reasons.HasFlag(EquipmentManager.EquipLimitReason.TierDifference))
            {
                lines.Add(
                    "• "
                        + L.S(
                            "paste_failed_reason_tier_diff",
                            "Some items are above the allowed tier difference for this troop."
                        )
                );
            }

            if (reasons.HasFlag(EquipmentManager.EquipLimitReason.Skill))
            {
                lines.Add(
                    "• "
                        + L.S(
                            "paste_failed_reason_skill",
                            "This troop does not meet the skill requirements for some items."
                        )
                );
            }

            var header = L.S(
                "paste_failed_not_allowed_header",
                "This equipment set cannot be applied to this troop:"
            );

            return header + "\n\n" + string.Join("\n", lines);
        }

        /// <summary>
        /// Select the previous equipment set.
        /// </summary>
        [DataSourceMethod]
        public void ExecutePrevSet()
        {
            var troop = State.Troop;
            var eq = State.Equipment;
            if (troop == null || eq == null)
                return;

            var list = EditCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;
            var idx = list.FindIndex(e => ReferenceEquals(e.Base, eq.Base));
            if (idx <= 0)
                return;

            State.UpdateEquipment(list[idx - 1]);
        }

        /// <summary>
        /// Select the next equipment set.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteNextSet()
        {
            var troop = State.Troop;
            var eq = State.Equipment;
            if (troop == null || eq == null)
                return;

            var list = EditCivilianSets ? troop.Loadout.CivilianSets : troop.Loadout.BattleSets;
            var idx = list.FindIndex(e => ReferenceEquals(e.Base, eq.Base));
            if (idx < 0 || idx >= list.Count - 1)
                return;

            State.UpdateEquipment(list[idx + 1]);
        }

        /// <summary>
        /// Remove the current equipment set after confirmation.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRemoveSet()
        {
            if (!CanRemoveSet)
                return;

            var troop = State.Troop;
            var eq = State.Equipment;
            if (troop == null || eq == null)
                return;

            var title = L.S("remove_set_title", "Remove Set");
            var text = L.T("remove_set_text", "Remove set n°{EQUIPMENT} for {TROOP}?")
                .SetTextVariable("EQUIPMENT", EquipmentName)
                .SetTextVariable("TROOP", troop.Name)
                .ToString();

            InformationManager.ShowInquiry(
                new InquiryData(
                    title,
                    text,
                    true,
                    true,
                    L.S("confirm", "Confirm"),
                    L.S("cancel", "Cancel"),
                    () =>
                    {
                        // Manager handles refunds and structural removal
                        var result = EquipmentManager.TryDeleteSet(troop, eq.Index);

                        // Keep masks valid and select canonical battle set (index 0)
                        State.FixCombatPolicies(troop);

                        // Pick next selection in the same category when possible
                        var next = EditCivilianSets
                            ? troop.Loadout.CivilianSets.FirstOrDefault()
                            : troop.Loadout.BattleSets.FirstOrDefault();

                        if (next == null)
                            next = troop.Loadout.Battle; // ultimate fallback

                        if (next != null)
                            State.UpdateEquipment(next);
                    },
                    () => { }
                )
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Create Set (free)                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteCreateSet()
        {
            if (!CanCreateSet)
                return;

            var troop = State.Troop;
            var src = State.Equipment; // current set
            if (troop == null || src == null)
                return;

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

        /// <summary>
        /// Create an empty set of the current type (battle/civilian),
        /// then apply mask defaults and select it.
        /// </summary>
        private void ExecuteCreateSet_EmptyFlow(WCharacter troop)
        {
            if (troop == null)
                return;

            var created = EditCivilianSets
                ? troop.Loadout.CreateCivilianSet()
                : troop.Loadout.CreateBattleSet();

            if (created == null)
                return;

            CombatAgentBehavior.DisableAll(troop, created.Index);
            State.FixCombatPolicies(troop);
            State.UpdateEquipment(created);
        }

        /// <summary>
        /// Copy the current set into a new one of the current type — always free.
        /// </summary>
        private void ExecuteCreateSet_CopyFlow(WCharacter troop, WEquipment src)
        {
            if (troop == null || src == null)
                return;

            var plan = CollectCopyPlan(src);

            var created = EditCivilianSets
                ? troop.Loadout.CreateCivilianSet()
                : troop.Loadout.CreateBattleSet();

            if (created == null)
                return;

            CopyItemsInto(created, plan);

            CombatAgentBehavior.DisableAll(troop, created.Index);
            State.FixCombatPolicies(troop);
            State.UpdateEquipment(created);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Build (slot, item) for all defined slots from the source equipment.
        /// </summary>
        private List<(EquipmentIndex slot, WItem item)> CollectCopyPlan(WEquipment src)
        {
            var list = new List<(EquipmentIndex, WItem)>(WEquipment.Slots.Count);
            foreach (var slot in WEquipment.Slots)
            {
                var it = src.Get(slot);
                if (it != null)
                    list.Add((slot, it));
            }
            return list;
        }

        /// <summary>
        /// Copy items (by slot) into the destination equipment (direct set, no staging).
        /// </summary>
        private static void CopyItemsInto(
            WEquipment dst,
            List<(EquipmentIndex slot, WItem item)> plan
        )
        {
            foreach (var (slot, item) in plan)
                dst.SetItem(slot, item);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Clipboard                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WEquipment Clipboard { get; set; }

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

            // Special case
            OnPropertyChanged(nameof(ShowEquipmentCheckboxes));
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

            OnPropertyChanged(nameof(ShowEquipmentCheckboxes));
        }
    }
}
