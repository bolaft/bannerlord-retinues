using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Staging;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
# if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.GUI.Editor.VM.Equipment.List
{
    /// <summary>
    /// ViewModel for a single equipment list row, handling selection and availability.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentRowVM(
        WItem rowItem,
        int cost,
        bool isAvailable,
        bool isUnlocked,
        int progress
    ) : BaseListElementVM(autoRegister: false)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly WItem RowItem = rowItem;
        public readonly bool IsAvailable = isAvailable;
        public readonly bool IsUnlocked = isUnlocked;
        public readonly int Progress = progress;
        public readonly int Cost = cost;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Note: row VMs do not autoregister to events, updates are made in event handlers below.
        protected override Dictionary<UIEvent, string[]> EventMap => [];

        /// <summary>
        /// Refresh slot-related bindings when the active slot changes.
        /// </summary>
        public void OnSlotChanged()
        {
            UpdateComparisonChevrons();
        }

        /// <summary>
        /// Refresh bindings affected by slot changes for the selected item.
        /// </summary>
        public void OnSlotChangedSelective()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }

        /// <summary>
        /// Refresh bindings affected by equip changes.
        /// </summary>
        public void OnEquipChanged()
        {
            UpdateComparisonChevrons();
        }

        /// <summary>
        /// Refresh bindings affected by staged equip for the selected item.
        /// </summary>
        public void OnEquipChangedSelective()
        {
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Stock));
            OnPropertyChanged(nameof(InStockText));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(IsDisabledText));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }

        /// <summary>
        /// Refresh bindings affected by equipment loadout changes.
        /// </summary>
        public void OnEquipmentChanged()
        {
            UpdateComparisonChevrons();

            OnPropertyChanged(nameof(ShowComparisonIcon));
            OnPropertyChanged(nameof(PositiveComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSpriteOffset));

            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(ShowIsEquipped));
            OnPropertyChanged(nameof(IsDisabledText));
            OnPropertyChanged(nameof(ShowInStockText));
            OnPropertyChanged(nameof(ShowValue));
            OnPropertyChanged(nameof(AvailableFromAnotherSet));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WItem Item => StagedItem ?? EquippedItem;

        private WItem EquippedItem => State.Equipment?.Get(State.Slot);

        private WItem StagedItem =>
            State.EquipData?.TryGetValue(State.Slot, out var equipData) == true
                ? equipData.Equip != null
                    ? new WItem(equipData.Equip.ItemId)
                    : null
                : null;

        private bool IsEquipped => !IsEmptyRow && EquippedItem == RowItem;
        private bool IsEmptyRow => RowItem == null;
        private bool IsRequirementBlocked =>
            !IsEmptyRow
            && EquipmentManager.MeetsItemSkillRequirements(State.Troop, RowItem) == false;
        private bool IsTierBlocked =>
            !IsEmptyRow
            && ClanScreen.EditorMode != EditorMode.Heroes // Heroes ignore tier restrictions
            && !DoctrineAPI.IsDoctrineUnlocked<Ironclad>()
            && (RowItem.Tier - (State.Troop?.Tier ?? 0)) > Config.AllowedTierDifference;
        private bool IsEquipmentTypeBlocked =>
            !IsEmptyRow && State.Equipment?.IsCivilian == true && RowItem?.IsCivilian == false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Values ━━━━━━━━ */

        [DataSourceProperty]
        public int Value => Cost;

        [DataSourceProperty]
        public int Stock => RowItem?.GetStock() ?? 0;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Name => RowItem?.Name ?? L.S("empty_item", "Empty");

        [DataSourceProperty]
        public string InStockText =>
            L.T("in_stock", "In Stock ({STOCK})").SetTextVariable("STOCK", Stock).ToString();

        [DataSourceProperty]
        public string IsDisabledText
        {
            get
            {
                if (!IsUnlocked)
                    return L.T("unlock_progress_text", "Unlocking ({PROGRESS}%)")
                        .SetTextVariable(
                            "PROGRESS",
                            (int)((float)Progress / Config.RequiredKillsPerItem * 100)
                        )
                        .ToString();

                if (!IsAvailable)
                    return Player.CurrentSettlement == null
                        ? L.S("item_unavailable_no_settlement", "Not in town")
                        : L.T("item_unavailable_text", "Not sold in {SETTLEMENT}")
                            .SetTextVariable("SETTLEMENT", Player.CurrentSettlement?.Name)
                            .ToString();

                if (IsRequirementBlocked)
                    return L.T("skill_requirement_text", "{SKILL}: {LEVEL}")
                        .SetTextVariable("SKILL", RowItem?.RelevantSkill?.Name)
                        .SetTextVariable("LEVEL", RowItem?.Difficulty ?? 0)
                        .ToString();

                if (IsTierBlocked)
                    return L.S("item_tier_blocked_text", "Tier too high");

                if (IsEquipmentTypeBlocked)
                    return L.S("item_equipment_type_blocked_text", "Not civilian");

                return string.Empty;
            }
        }

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowIsEquipped =>
            (StagedItem != null && IsEquipped)
            || (AvailableFromAnotherSet && !IsEquipmentTypeBlocked);

        [DataSourceProperty]
        public bool ShowInStockText =>
            !ClanScreen.IsStudioMode
            && Config.EquippingTroopsCostsGold
            && IsEnabled
            && !IsSelected
            && !IsEquipped
            && !AvailableFromAnotherSet
            && RowItem?.IsStocked == true;

        [DataSourceProperty]
        public bool ShowValue =>
            !ClanScreen.IsStudioMode
            && Config.EquippingTroopsCostsGold
            && IsEnabled
            && !IsSelected
            && !IsEquipped
            && !AvailableFromAnotherSet
            && !ShowInStockText
            && Value > 0;

        [DataSourceProperty]
        public override bool IsSelected => RowItem == Item;

        [DataSourceProperty]
        public override bool IsEnabled =>
            IsEmptyRow
            || (
                IsUnlocked
                && IsAvailable
                && !IsRequirementBlocked
                && !IsTierBlocked
                && !IsEquipmentTypeBlocked
            );

        [DataSourceProperty]
        public bool AvailableFromAnotherSet
        {
            get
            {
                if (RowItem == null || State.Troop == null || State.Equipment == null)
                    return false;

                var loadout = State.Troop.Loadout;
                // current set has 0 of this item…
                int inThisSet = loadout.CountInSet(RowItem, State.Equipment.Index);
                if (inThisSet > 0)
                    return false;

                // …and at least one other set has >= 1 (i.e., free to share one)
                return loadout.MaxCountPerSet(RowItem) >= 1;
            }
        }

        /* ━━━━━━━━━ Comparison Icons ━━━━━━━━ */

        /// <summary>
        /// Update the positive/negative comparison chevron counts.
        /// </summary>
        private void UpdateComparisonChevrons()
        {
            PositiveChevrons = 0;
            NegativeChevrons = 0;

            if (!Config.EnableItemComparisonIcons)
                return;

            if (!IsEnabled)
                return;

            try
            {
                RowItem?.GetComparisonChevrons(Item, out PositiveChevrons, out NegativeChevrons);
            }
            catch (System.Exception e)
            {
                Log.Error(
                    $"EquipmentRowVM.GetChevronCounts failed for RowItem={RowItem}, Current={Item}: {e}"
                );
            }

            OnPropertyChanged(nameof(ShowComparisonIcon));
            OnPropertyChanged(nameof(PositiveComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSprite));
            OnPropertyChanged(nameof(NegativeComparisonSpriteOffset));
        }

        private int PositiveChevrons = 0;
        private int NegativeChevrons = 0;

        [DataSourceProperty]
        public bool ShowComparisonIcon => PositiveChevrons > 0 || NegativeChevrons > 0;

        [DataSourceProperty]
        public string PositiveComparisonSprite
        {
            get
            {
                if (PositiveChevrons <= 0)
                    return string.Empty;

                if (PositiveChevrons > 3)
                    PositiveChevrons = 3;

                return $"General\\TroopTierIcons\\icon_tier_{PositiveChevrons}_big";
            }
        }

        [DataSourceProperty]
        public string NegativeComparisonSprite
        {
            get
            {
                if (NegativeChevrons <= 0)
                    return string.Empty;

                if (NegativeChevrons > 3)
                    NegativeChevrons = 3;

                return $"General\\TroopTierIcons\\icon_tier_{NegativeChevrons}_big";
            }
        }

        [DataSourceProperty]
        public int NegativeComparisonSpriteOffset
        {
            get
            {
                if (NegativeChevrons > 0 && PositiveChevrons > 0)
                    return 5;

                return 0;
            }
        }

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public string ImageId => RowItem?.Image?.Id;

        [DataSourceProperty]
        public string BannerId => RowItem?.Culture?.Image?.Id;

        [DataSourceProperty]
        public string ImageAdditionalArgs => RowItem?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string BannerAdditionalArgs => RowItem?.Culture?.Image?.AdditionalArgs;

#if BL13
        [DataSourceProperty]
        public string ImageTextureProviderName => RowItem?.Image?.TextureProviderName;

        [DataSourceProperty]
        public string BannerTextureProviderName => RowItem?.Culture?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public int ImageTypeCode => RowItem?.Image?.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public int BannerTypeCode => RowItem?.Culture?.Image?.ImageTypeCode ?? 0;
#endif

        /* ━━━━━━━━ Tooltip ━━━━━━━ */

        [DataSourceProperty]
        public CharacterEquipmentItemVM Hint
        {
            get
            {
                if (RowItem == null)
                    return null;
                var vm = new CharacterEquipmentItemVM(RowItem.Base);
                return vm;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handle selection of this equipment row.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelect()
        {
            if (State.Troop == null || State.Equipment == null)
            {
                Log.Error("ExecuteSelect: Aborting - Troop or Equipment is null");
                return;
            }

            // Enter
            Log.Debug(
                $"ExecuteSelect: Start RowItem={RowItem?.Name?.ToString() ?? "null"}, Slot={State.Slot}, SetIndex={State.Equipment?.Index.ToString() ?? "null"}"
            );

            // Slot sanity
            if (RowItem != null && !RowItem.Slots.Contains(State.Slot))
            {
                Log.Debug("ExecuteSelect: Aborting - RowItem does not fit slot");
                return;
            }

            // Context restriction (only for instant mode)
            if (!Config.EquippingTroopsTakesTime)
                if (
                    !ContextManager.IsAllowedInContextWithPopup(
                        State.Troop,
                        L.S("action_modify", "modify")
                    )
                )
                {
                    Log.Debug("ExecuteSelect: Aborting - Context not allowed for modification");
                    return;
                }

            var troop = State.Troop;
            var setIndex = State.Equipment.Index;
            var slot = State.Slot;

            var equippedItem = State.Equipment.Get(slot);
            var selectionIsNull = RowItem == null;
            var selectionIsEquipped = RowItem != null && RowItem == equippedItem;

            PendingEquipData pending = null;
            WItem stagedItem = null;

            pending = EquipStagingBehavior.Get(troop, slot, setIndex);
            if (pending != null)
                stagedItem = new WItem(pending.ItemId);

            var hasPending = pending != null;

            Log.Debug(
                $"ExecuteSelect: Computed state - Troop={troop?.ToString() ?? "null"}, EquippedItem={equippedItem?.Name?.ToString() ?? "null"}, selectionIsNull={selectionIsNull}, selectionIsEquipped={selectionIsEquipped}"
            );

            // Studio: bypass rules/costs/time
            if (ClanScreen.IsStudioMode)
            {
                Log.Debug(
                    "ExecuteSelect: Studio mode - calling EquipmentManager.TryEquip (allowPurchase:false)"
                );
                var res = EquipmentManager.TryEquip(
                    troop,
                    setIndex,
                    slot,
                    RowItem,
                    allowPurchase: false
                );
                Log.Debug(
                    $"ExecuteSelect: TryEquip (studio) result - Ok={res.Ok}, Reason={res.Reason}, Staged={res.Staged}, RefundedCopies={res.RefundedCopies}, AddedCopies={res.AddedCopies}, GoldDelta={res.GoldDelta}"
                );
                // (res.Staged will be false in studio, by design)
                State.UpdateEquipData();
                return;
            }

            // Case 1: Unequip
            if (selectionIsNull)
            {
                if (hasPending && stagedItem != null)
                {
                    Log.Debug(
                        "ExecuteSelect: Empty row clicked with pending change - rolling back staged equip and unstaging."
                    );

                    EquipmentManager.RollbackStagedEquip(troop, setIndex, slot, stagedItem);

                    pending = null;
                }

                // Only warn if reverting would later take time.
                // That’s exactly when this unequip reduces required copies (deltaRemove > 0).
                if (
                    !ClanScreen.IsStudioMode
                    && Config.EquippingTroopsTakesTime
                    && equippedItem != null
                )
                {
                    var loadout = troop.Loadout;
                    int beforeOld = loadout.MaxCountPerSet(equippedItem);
                    int afterOld = loadout.RequiredAfterForItem(equippedItem, setIndex, slot, null);
                    bool revertWouldStage = afterOld < beforeOld; // deltaRemove > 0

                    Log.Debug(
                        $"ExecuteSelect: Unequip path - revertWouldStage={revertWouldStage}, beforeOld={beforeOld}, afterOld={afterOld}"
                    );

                    if (revertWouldStage)
                    {
                        Log.Debug("ExecuteSelect: Showing unequip warning inquiry");
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                L.S("unequip_warn_title", "Unequip Item"),
                                L.T(
                                        "unequip_warning_text",
                                        "Unequipping is instant, but equipping a different item later may take time.\n\nConfirm?"
                                    )
                                    .ToString(),
                                true,
                                true,
                                L.S("confirm", "Confirm"),
                                L.S("cancel", "Cancel"),
                                () =>
                                {
                                    Log.Debug(
                                        "ExecuteSelect: Unequip warning confirmed - calling EquipmentManager.TryUnequip"
                                    );
                                    var res = EquipmentManager.TryUnequip(troop, setIndex, slot);
                                    Log.Debug(
                                        $"ExecuteSelect: TryUnequip result - Ok={res.Ok}, Reason={res.Reason}, Staged={res.Staged}, RefundedCopies={res.RefundedCopies}, AddedCopies={res.AddedCopies}, GoldDelta={res.GoldDelta}"
                                    );
                                    State.UpdateEquipData();
                                },
                                () =>
                                {
                                    Log.Debug("ExecuteSelect: Unequip warning cancelled by user");
                                }
                            )
                        );
                        return;
                    }
                }

                // No warning needed: either studio, equip-changes don’t take time,
                // or this unequip does not reduce required copies.
                {
                    Log.Debug(
                        "ExecuteSelect: Unequip without warning - calling EquipmentManager.TryUnequip"
                    );
                    var res = EquipmentManager.TryUnequip(troop, setIndex, slot);
                    Log.Debug(
                        $"ExecuteSelect: TryUnequip result - Ok={res.Ok}, Reason={res.Reason}, Staged={res.Staged}, RefundedCopies={res.RefundedCopies}, AddedCopies={res.AddedCopies}, GoldDelta={res.GoldDelta}"
                    );
                    State.UpdateEquipData();
                    return;
                }
            }

            // Case 2: Already equipped -> no-op (but still refresh to collapse any staged visuals)
            if (selectionIsEquipped)
            {
                // If a different item is staged for this slot,
                // treat clicking the equipped row as "cancel staged change".
                if (hasPending && stagedItem != null)
                {
                    Log.Debug(
                        "ExecuteSelect: Equipped item clicked with pending change - rolling back staged equip and unstaging."
                    );

                    EquipmentManager.RollbackStagedEquip(troop, setIndex, slot, stagedItem);

                    State.UpdateEquipData();
                    return;
                }

                Log.Debug("ExecuteSelect: Selection is already equipped - refreshing state");
                State.UpdateEquipData();
                return;
            }

            // Case 3: Equip a different item
            // Preview the change to drive UI flow (confirm cost if needed).
            if (hasPending && stagedItem != null)
            {
                Log.Debug(
                    "ExecuteSelect: New item clicked while a staged change exists - rolling back previous staged equip and unstaging."
                );

                EquipmentManager.RollbackStagedEquip(troop, setIndex, slot, stagedItem);

                // Refresh pending state
                pending = null;
                hasPending = false;
            }
            var quote = EquipmentManager.QuoteEquip(troop, setIndex, slot, RowItem);
            Log.Debug(
                $"ExecuteSelect: Quote - IsChange={quote.IsChange}, CopiesToBuy={quote.CopiesToBuy}, GoldCost={quote.GoldCost}"
            );

            // No structural change (defensive)
            if (!quote.IsChange)
            {
                Log.Debug("ExecuteSelect: Quote indicates no change - refreshing state");
                State.UpdateEquipData();
                return;
            }

            // If we must buy copies (and player pays for equipment), ask for confirmation
            bool needsPurchase = quote.CopiesToBuy > 0 && quote.GoldCost > 0;
            Log.Debug($"ExecuteSelect: needsPurchase={needsPurchase}");
            if (needsPurchase)
            {
                Log.Debug("ExecuteSelect: Showing purchase confirmation inquiry");
                InformationManager.ShowInquiry(
                    new InquiryData(
                        L.S("buy_item", "Buy Item"),
                        L.T(
                                "buy_item_text",
                                "Are you sure you want to buy {ITEM_NAME} for {ITEM_VALUE} gold?"
                            )
                            .SetTextVariable("ITEM_NAME", RowItem.Name)
                            .SetTextVariable("ITEM_VALUE", quote.GoldCost)
                            .ToString(),
                        true,
                        true,
                        L.S("yes", "Yes"),
                        L.S("no", "No"),
                        () =>
                        {
                            Log.Debug(
                                "ExecuteSelect: Purchase confirmed - calling EquipmentManager.TryEquip (allowPurchase:true)"
                            );
                            var res = EquipmentManager.TryEquip(
                                troop,
                                setIndex,
                                slot,
                                RowItem,
                                allowPurchase: true
                            );
                            Log.Debug(
                                $"ExecuteSelect: TryEquip (purchase) result - Ok={res.Ok}, Reason={res.Reason}, Staged={res.Staged}, RefundedCopies={res.RefundedCopies}, AddedCopies={res.AddedCopies}, GoldDelta={res.GoldDelta}, Reason={(res.Ok ? "Ok" : res.Reason.ToString())}"
                            );
                            if (
                                !res.Ok
                                && res.Reason == EquipmentManager.EquipFailReason.NotEnoughGold
                            )
                            {
                                Notifications.Popup(
                                    L.T("not_enough_gold_title", "Not Enough Gold"),
                                    L.T(
                                        "not_enough_gold_text",
                                        "You do not have enough gold to purchase this item."
                                    )
                                );
                                Log.Debug("ExecuteSelect: TryEquip failed - NotEnoughGold");
                            }
                            // res.Staged indicates if it went into staging (only DeltaAdd > 0 and option on)
                            State.UpdateEquipData();
                        },
                        () =>
                        {
                            Log.Debug("ExecuteSelect: Purchase cancelled by user");
                        }
                    )
                );
                return;
            }

            // Otherwise, try equip directly (free, stocked, or cross-set share)
            {
                Log.Debug(
                    "ExecuteSelect: Direct equip path - calling EquipmentManager.TryEquip (allowPurchase:true)"
                );
                var res = EquipmentManager.TryEquip(
                    troop,
                    setIndex,
                    slot,
                    RowItem,
                    allowPurchase: true
                );
                Log.Debug(
                    $"ExecuteSelect: TryEquip (direct) result - Ok={res.Ok}, Reason={res.Reason}, Staged={res.Staged}, RefundedCopies={res.RefundedCopies}, AddedCopies={res.AddedCopies}, GoldDelta={res.GoldDelta}"
                );
                State.UpdateEquipData();
                return;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determine whether this equipment row matches the provided filter.
        /// </summary>
        public override bool FilterMatch(string filter)
        {
            if (RowItem == null)
                return true;

            var search = filter.Trim().ToLowerInvariant();
            var name = RowItem.Name.ToString().ToLowerInvariant();
            var category = RowItem.Category?.ToString().ToLowerInvariant();
            var type = RowItem.Type.ToString().ToLowerInvariant();
            var culture = RowItem.Culture?.Name?.ToString().ToLowerInvariant() ?? string.Empty;

            return name.Contains(search)
                || category.Contains(search)
                || type.Contains(search)
                || culture.Contains(search);
        }
    }
}
