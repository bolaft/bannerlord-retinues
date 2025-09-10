using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopEditorVM(EditorScreenVM screen)
        : BaseEditor<TroopEditorVM>(screen),
            IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly MBBindingList<TroopSkillVM> _skillsRow1 = [];
        private readonly MBBindingList<TroopSkillVM> _skillsRow2 = [];

        private readonly MBBindingList<TroopConversionRowVM> _conversionRows = [];
        private readonly Dictionary<string, StagedOrder> _staged = [];

        private sealed class StagedOrder
        {
            public WCharacter From;
            public WCharacter To;
            public int Amount;
            public int CostPer => CostPerUnit(From, To);
            public int TotalCost => Amount * CostPer;
            public string Key => $"{From?.StringId}->{To?.StringId}";
        }

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public bool IsRetinue => SelectedTroop.IsRetinue;

        [DataSourceProperty]
        public bool IsRetinueIsNotMaxTier => !IsMaxTier && IsRetinue;

        [DataSourceProperty]
        public bool IsRegular => !SelectedTroop.IsRetinue;

        [DataSourceProperty]
        public int TroopCount => GetVirtualCount(SelectedTroop);

        [DataSourceProperty]
        public int RetinueCap => TroopRules.RetinueCapFor(SelectedTroop);

        [DataSourceProperty]
        public string Name => SelectedTroop?.Name;

        [DataSourceProperty]
        public string Tier
        {
            get
            {
                int? tier = SelectedTroop?.Tier;
                return tier switch
                {
                    1 => "I",
                    2 => "II",
                    3 => "III",
                    4 => "IV",
                    5 => "V",
                    6 => "VI",
                    _ => string.Empty,
                };
            }
        }

        [DataSourceProperty]
        public string Gender => SelectedTroop != null && SelectedTroop.IsFemale ? "Female" : "Male";

        [DataSourceProperty]
        public int SkillTotal =>
            SelectedTroop != null ? TroopRules.SkillTotalByTier(SelectedTroop.Tier) : 0;

        [DataSourceProperty]
        public int SkillPointsUsed => SelectedTroop?.Skills.Values.Sum() ?? 0;

        [DataSourceProperty]
        public bool CanRankUp =>
            IsRetinue && TroopRules.SkillPointsLeft(SelectedTroop) == 0 && !SelectedTroop.IsMaxTier;

        [DataSourceProperty]
        public bool IsMaxTier => SelectedTroop?.IsMaxTier ?? false;

        [DataSourceProperty]
        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets
        {
            get
            {
                var upgrades = new MBBindingList<TroopUpgradeTargetVM>();

                if (SelectedTroop != null)
                    foreach (var target in SelectedTroop.UpgradeTargets)
                        upgrades.Add(new TroopUpgradeTargetVM(target));

                return upgrades;
            }
        }

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow1 => _skillsRow1;

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow2 => _skillsRow2;

        [DataSourceProperty]
        public MBBindingList<TroopConversionRowVM> ConversionRows => _conversionRows;

        [DataSourceProperty]
        public bool CanUpgrade =>
            SelectedTroop != null && TroopRules.CanUpgradeTroop(SelectedTroop);

        [DataSourceProperty]
        public bool CanRecruit => true;

        [DataSourceProperty]
        public bool CanRemove
        {
            get
            {
                if (Screen.IsEquipmentMode)
                    return false; // Only show in default mode
                if (SelectedTroop?.Parent is null)
                    return false; // Cannot remove root troops
                if (SelectedTroop?.UpgradeTargets.Count() > 0)
                    return false; // Cannot remove troops that are upgrade targets

                return true;
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (CanRemove)
                    return null; // No hint if can remove

                return Helpers.Tooltip.MakeTooltip(null, CantRemoveTroopExplanation);
            }
        }

        [DataSourceProperty]
        public bool HasPendingConversions => _staged.Count > 0;

        [DataSourceProperty]
        public int PendingTotalCost => _staged.Values.Sum(o => o.TotalCost);

        [DataSourceProperty]
        public int PendingTotalCount => _staged.Values.Sum(o => o.Amount);

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteRankUp()
        {
            if (SelectedTroop == null)
                return;
            if (!CanRankUp)
                return;

            int cost = TroopRules.RankUpCost(SelectedTroop);

            if (Player.Gold < cost)
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        titleText: "Not enough gold",
                        text: $"You do not have enough gold to rank up {SelectedTroop.Name}.\n\nRank up cost: {cost} gold.",
                        isAffirmativeOptionShown: false,
                        isNegativeOptionShown: true,
                        affirmativeText: null,
                        negativeText: "OK",
                        affirmativeAction: null,
                        negativeAction: () => { }
                    )
                );
            }
            else
            {
                InformationManager.ShowInquiry(
                    new InquiryData(
                        titleText: "Rank Up",
                        text: $"Increase {SelectedTroop.Name}'s tier?\n\nIt will cost you {cost} gold.",
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: true,
                        affirmativeText: "Confirm",
                        negativeText: "Cancel",
                        affirmativeAction: () =>
                        {
                            TroopManager.RankUp(SelectedTroop);

                            // UI updates
                            Refresh();
                            _screen.TroopList.SelectedRow.Refresh();
                        },
                        negativeAction: () => { }
                    )
                );
            }
        }

        [DataSourceMethod]
        public void ExecuteRename()
        {
            var oldName = SelectedTroop.Name;

            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    titleText: "Rename Troop",
                    text: "Enter a new name:",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Confirm",
                    negativeText: "Cancel",
                    affirmativeAction: newName =>
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                            return;

                        TroopManager.Rename(SelectedTroop, newName);

                        // UI updates
                        OnPropertyChanged(nameof(Name));
                        foreach (var row in ConversionRows)
                            row.Refresh(); // Update conversion rows

                        Screen.TroopList.SelectedRow.Refresh();
                    },
                    negativeAction: () => { },
                    defaultInputText: oldName
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            TroopManager.ChangeGender(SelectedTroop);

            // UI updates
            OnPropertyChanged(nameof(Gender));
            Screen.TroopList.SelectedRow.Refresh();
            Screen.Refresh();
        }

        [DataSourceMethod]
        public void ExecuteAddUpgradeTarget()
        {
            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    titleText: "Add Upgrade",
                    text: "Enter the name of the new troop:",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Confirm",
                    negativeText: "Cancel",
                    affirmativeAction: name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        var target = TroopManager.AddUpgradeTarget(SelectedTroop, name);

                        // Update the troop list
                        Screen.TroopList.Refresh();

                        // Select the new troop
                        Screen.TroopList.Select(target);
                    },
                    negativeAction: () => { },
                    defaultInputText: SelectedTroop.Name
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: "Remove Troop",
                    text: $"Are you sure you want to permanently remove {SelectedTroop.Name}?\n\nTheir equipment will be stocked for later use.",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: "Confirm",
                    negativeText: "Cancel",
                    affirmativeAction: () =>
                    {
                        TroopManager.Remove(SelectedTroop);

                        // Update the troop list
                        Screen.TroopList.Refresh();
                    },
                    negativeAction: () => { }
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteApplyConversions()
        {
            if (!HasPendingConversions)
                return;

            int totalCost = PendingTotalCost;
            if (totalCost > Player.Gold)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("Not enough gold to apply conversions.")
                );
                return;
            }

            // Re-validate & apply in a deterministic order
            foreach (var order in _staged.Values.ToList())
            {
                // Clamp to current max (no virtuals now; roster will change as we go)
                int maxNow = TroopManager.GetMaxConvertible(order.From, order.To);
                int amount = System.Math.Min(order.Amount, maxNow);
                if (amount <= 0)
                    continue;

                // Deduct gold for recruit-to-retinue only
                int costPer = CostPerUnit(order.From, order.To);
                int cost = amount * costPer;
                if (cost > 0)
                {
                    if (cost > Player.Gold)
                    {
                        InformationManager.ShowInquiry(
                            new InquiryData(
                                "Not enough gold",
                                "You do not have enough gold to hire these retinues.",
                                false,
                                true,
                                null,
                                "OK",
                                null,
                                null
                            )
                        );
                        break;
                    }
                }

                TroopManager.Convert(order.From, order.To, amount, cost);
            }

            _staged.Clear();
            Refresh(); // full UI refresh (rows recount, caps, etc.)
        }

        [DataSourceMethod]
        public void ExecuteClearConversions()
        {
            if (!HasPendingConversions)
                return;
            _staged.Clear();
            // push UI updates
            OnPropertyChanged(nameof(HasPendingConversions));
            OnPropertyChanged(nameof(PendingTotalCost));
            OnPropertyChanged(nameof(PendingTotalCount));
            // rows need to recompute virtuals
            foreach (var row in _conversionRows)
                row.Refresh();
        }

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            Log.Debug("Refreshing.");

            RebuildSkillRows();
            RebuildRetinueRows();

            OnPropertyChanged(nameof(TroopCount));
            OnPropertyChanged(nameof(RetinueCap));

            OnPropertyChanged(nameof(IsRetinue));
            OnPropertyChanged(nameof(IsRegular));
            OnPropertyChanged(nameof(IsRetinueIsNotMaxTier));

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SkillTotal));
            OnPropertyChanged(nameof(SkillPointsUsed));
            OnPropertyChanged(nameof(Tier));
            OnPropertyChanged(nameof(IsMaxTier));
            OnPropertyChanged(nameof(UpgradeTargets));

            OnPropertyChanged(nameof(CanUpgrade));
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(CanRankUp));
            OnPropertyChanged(nameof(RemoveButtonHint));

            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));

            OnPropertyChanged(nameof(ConversionRows));
            OnPropertyChanged(nameof(HasPendingConversions));
            OnPropertyChanged(nameof(PendingTotalCost));
            OnPropertyChanged(nameof(PendingTotalCount));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private string CantRemoveTroopExplanation =>
            SelectedTroop?.Parent is null ? "Root troops cannot be removed."
            : SelectedTroop?.UpgradeTargets.Count() > 0
                ? "Troops that have upgrade targets cannot be removed."
            : string.Empty;

        private void RebuildSkillRows()
        {
            Log.Debug("Rebuilding skill rows.");

            _skillsRow1.Clear();
            _skillsRow2.Clear();

            if (SelectedTroop == null)
                return;

            foreach (var s in SelectedTroop.Skills.Take(4))
                _skillsRow1.Add(new TroopSkillVM(s.Key, SelectedTroop, this));

            foreach (var s in SelectedTroop.Skills.Skip(4).Take(4))
                _skillsRow2.Add(new TroopSkillVM(s.Key, SelectedTroop, this));
        }

        private void RebuildRetinueRows()
        {
            _conversionRows.Clear();
            if (SelectedTroop == null)
                return;

            if (SelectedTroop.IsRetinue)
            {
                // From regulars in party → this retinue
                foreach (var troop in TroopManager.GetRetinueSourceTroops(SelectedTroop))
                    _conversionRows.Add(new TroopConversionRowVM(troop, SelectedTroop, this));
            }
        }

        internal int GetVirtualCount(WCharacter c)
        {
            int count = Player.Party.MemberRoster.CountOf(c);
            foreach (var o in _staged.Values)
            {
                if (o.To == c)
                    count += o.Amount;
                if (o.From == c)
                    count -= o.Amount;
            }
            return count;
        }

        internal int GetPendingAmount(WCharacter from, WCharacter to)
        {
            var key = $"{from?.StringId}->{to?.StringId}";
            return _staged.TryGetValue(key, out var o) ? o.Amount : 0;
        }

        internal int GetMaxStageable(WCharacter from, WCharacter to)
        {
            if (from == null || to == null)
                return 0;

            // availability from virtual roster
            int availableFrom = GetVirtualCount(from);
            if (availableFrom <= 0)
                return 0;

            // if converting into a retinue, respect cap using virtual "to" count
            if (to.IsRetinue)
            {
                int cap = TroopRules.RetinueCapFor(to);
                int currentTo = GetVirtualCount(to);
                int capLeft = System.Math.Max(0, cap - currentTo);
                availableFrom = System.Math.Min(availableFrom, capLeft);

                // also respect gold virtually
                int costPer = CostPerUnit(from, to);
                if (costPer > 0)
                {
                    int alreadyCost = PendingTotalCost;
                    int goldLeft = System.Math.Max(0, Player.Gold - alreadyCost);
                    int byGold = costPer > 0 ? goldLeft / costPer : availableFrom;
                    availableFrom = System.Math.Min(availableFrom, byGold);
                }
            }

            return System.Math.Max(0, availableFrom);
        }

        internal void StageConversion(WCharacter from, WCharacter to, int amountRequested)
        {
            int max = GetMaxStageable(from, to);
            int amount = System.Math.Min(amountRequested, max);
            if (amount <= 0)
                return;

            string key = $"{from.StringId}->{to.StringId}";
            if (!_staged.TryGetValue(key, out var order))
            {
                order = new StagedOrder
                {
                    From = from,
                    To = to,
                    Amount = 0,
                };
                _staged[key] = order;
            }
            order.Amount += amount;

            // Update staging summary + rows (virtuals changed)
            OnPropertyChanged(nameof(HasPendingConversions));
            OnPropertyChanged(nameof(PendingTotalCost));
            OnPropertyChanged(nameof(PendingTotalCount));

            foreach (var row in _conversionRows)
                row.Refresh();
        }

        internal static int CostPerUnit(WCharacter from, WCharacter to)
        {
            // Only charge when the result is a retinue (release is free; change if you want refund/fee).
            return to.IsRetinue ? TroopRules.ConversionCostPerUnit(to) : 0;
        }
    }
}
