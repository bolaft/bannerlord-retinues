using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Features.Xp.Behaviors;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for troop editor. Handles skill editing, upgrades, conversions, retinue logic, and UI actions.
    /// </summary>
    [SafeClass]
    public sealed class TroopPanelVM(WCharacter troop, WFaction faction) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        private readonly WCharacter _troop = troop;
        private readonly WFaction _faction = faction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Headers ━━━━━━━ */

        [DataSourceProperty]
        public string GenderHeaderText => L.S("gender_header_text", "Gender");

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [DataSourceProperty]
        public string SkillsHeaderText => L.S("skills_header_text", "Skills");

        [DataSourceProperty]
        public string UpgradesHeaderText => L.S("upgrades_header_text", "Upgrades");

        [DataSourceProperty]
        public string TransferHeaderText => L.S("transfer_header_text", "Transfer");

        /* ━━━━━━━━ Removal ━━━━━━━ */

        [DataSourceProperty]
        public bool RemoveTroopButtonIsVisible =>
            Editor.Mode == EditorMode.Troop && !_troop.IsRetinue && !_troop.IsMilitia;

        [DataSourceProperty]
        public bool RemoveTroopButtonIsEnabled =>
            Editor.Mode == EditorMode.Troop && _troop.IsDeletable;

        [DataSourceProperty]
        public string RemoveButtonText => L.S("remove_button_text", "Remove");

        [DataSourceProperty]
        public BasicTooltipViewModel RemoveButtonHint
        {
            get
            {
                if (CanRemove)
                    return null; // No hint if can remove

                return Tooltip.MakeTooltip(
                    null,
                    _troop?.Parent is null
                            ? L.S("cant_remove_root_troop", "Root troops cannot be removed.")
                        : _troop?.UpgradeTargets.Count() > 0
                            ? L.S(
                                "cant_remove_troop_with_targets",
                                "Troops that have upgrade targets cannot be removed."
                            )
                        : string.Empty
                );
            }
        }

        [DataSourceProperty]
        public bool CanRemove
        {
            get
            {
                if (_troop.Parent is null)
                    return false; // Cannot remove root troops
                if (_troop.UpgradeTargets.Count() > 0)
                    return false; // Cannot remove troops that are upgrade targets
                if (_troop.IsRetinue == true || _troop.IsMilitia == true)
                    return false; // Cannot remove retinues or militia

                return true;
            }
        }

        /* ━━━━━━━ Character ━━━━━━ */

        [DataSourceProperty]
        public string Name => Format.Crop(_troop.Name, 35);

        [DataSourceProperty]
        public string Gender => _troop.IsFemale ? L.S("female", "Female") : L.S("male", "Male");

        [DataSourceProperty]
        public string TierText
        {
            get
            {
                string roman = _troop.Tier switch
                {
                    0 => "0",
                    1 => "I",
                    2 => "II",
                    3 => "III",
                    4 => "IV",
                    5 => "V",
                    6 => "VI",
                    7 => "VII",
                    8 => "VIII",
                    9 => "IX",
                    _ => string.Empty,
                };
                return $"{L.S("tier", "Tier")} {roman}";
            }
        }

        /* ━━━━━━━━ Rank Up ━━━━━━━ */

        [DataSourceProperty]
        public bool CanRankUp => _troop.IsRetinue && _troop.IsMaxTier;

        /* ━━━━━━ Experience ━━━━━━ */

        [DataSourceProperty]
        public bool TroopXpIsEnabled =>
            Config.BaseSkillXpCost > 0 || Config.SkillXpCostPerPoint > 0;

        [DataSourceProperty]
        public string TroopXpText =>
            L.T("troop_xp", "{XP} xp")
                .SetTextVariable("XP", TroopXpBehavior.Get(_troop))
                .ToString();

        /* ━━━━━━━━ Skills ━━━━━━━━ */

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow1
        {
            get
            {
                var list = new MBBindingList<TroopSkillVM>();
                foreach (var s in _troop.Skills.Take(4))
                    list.Add(new TroopSkillVM(_troop, s.Key));
                return list;
            }
        }

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow2
        {
            get
            {
                var list = new MBBindingList<TroopSkillVM>();
                foreach (var s in _troop.Skills.Skip(4).Take(4))
                    list.Add(new TroopSkillVM(_troop, s.Key));
                return list;
            }
        }

        [DataSourceProperty]
        public string SkillCapText
        {
            get
            {
                int cap = TroopRules.SkillCapByTier(_troop);
                return L.T("skill_cap_text", "{CAP} skill cap")
                    .SetTextVariable("CAP", cap)
                    .ToString();
            }
        }

        [DataSourceProperty]
        public int SkillPointsTotal => TroopRules.SkillTotalByTier(_troop);

        [DataSourceProperty]
        public int SkillPointsUsed =>
            _troop.Skills.Values.Sum()
            + TroopTrainBehavior.Instance.GetPending(_troop.StringId).Sum(d => d.PointsRemaining);

        /* ━━━━━━━ Training ━━━━━━━ */

        [DataSourceProperty]
        public bool TrainingTakesTime => Config.TrainingTakesTime;

        [DataSourceProperty]
        public bool TrainingIsRequired => TrainingRequired > 0;

        [DataSourceProperty]
        public int TrainingRequired =>
            TroopTrainBehavior.Instance.GetPending(_troop.StringId)?.Sum(data => data.Remaining)
            ?? 0;

        [DataSourceProperty]
        public string TrainingRequiredText
        {
            get
            {
                if (TrainingIsRequired == false)
                    return L.S("no_training_required", "No training required");

                return L.T("training_required", "{HOURS} hours of training required")
                    .SetTextVariable("HOURS", TrainingRequired)
                    .ToString();
            }
        }

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceProperty]
        public bool CanUpgrade => TroopRules.CanUpgradeTroop(_troop);

        [DataSourceProperty]
        public string UpgradeButtonText => L.S("add_upgrade_button_text", "Add Upgrade");

        [DataSourceProperty]
        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets
        {
            get
            {
                var upgrades = new MBBindingList<TroopUpgradeTargetVM>();

                foreach (var target in _troop.UpgradeTargets)
                    upgrades.Add(new TroopUpgradeTargetVM(target));

                return upgrades;
            }
        }

        /* ━━━━━━ Conversion ━━━━━━ */

        [DataSourceProperty]
        public MBBindingList<TroopConversionRowVM> ConversionRows
        {
            get
            {
                var list = new MBBindingList<TroopConversionRowVM>();
                if (_troop.IsRetinue)
                    foreach (var troop in TroopManager.GetRetinueSourceTroops(_troop))
                        list.Add(new TroopConversionRowVM(this, troop, _troop));
                return list;
            }
        }

        [DataSourceProperty]
        public string ButtonApplyConversionsText => L.S("apply_conversions_button_text", "Convert");

        [DataSourceProperty]
        public string ButtonClearConversionsText => L.S("clear_conversions_button_text", "Clear");

        [DataSourceProperty]
        public bool HasPendingConversions => _staged.Count > 0;

        [DataSourceProperty]
        public int PendingTotalCost => _staged.Values.Sum(o => o.TotalCost);

        [DataSourceProperty]
        public int PendingTotalCount => _staged.Values.Sum(o => o.Amount);

        [DataSourceProperty]
        public int TroopCount => GetVirtualCount(_troop);

        [DataSourceProperty]
        public int RetinueCap => TroopRules.RetinueCapFor(_troop);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Character ━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRename()
        {
            var oldName = _troop.Name;

            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    titleText: L.S("rename_troop", "Rename Troop"),
                    text: L.S("enter_new_name", "Enter a new name:"),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: newName =>
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                            return;

                        TroopManager.Rename(_troop, newName);

                        // Refresh bindings
                        OnPropertyChanged(nameof(Name));
                        OnPropertyChanged(nameof(Editor.TroopList.Selection.NameText));
                    },
                    negativeAction: () => { },
                    defaultInputText: oldName
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            TroopManager.ChangeGender(_troop);

            // Refresh bindings
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(Editor.Model));
        }

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteAddUpgradeTarget()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _troop,
                    _faction,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Modification not allowed in current context

            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    titleText: L.S("add_upgrade", "Add Upgrade"),
                    text: L.S("enter_new_troop_name", "Enter the name of the new troop:"),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        var target = TroopManager.AddUpgradeTarget(_troop, name);

                        // Select the new troop
                        Editor.TroopList.Select(target);
                    },
                    negativeAction: () => { },
                    defaultInputText: _troop.Name
                )
            );
        }

        /* ━━━━━━━━ Removal ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRemoveTroop()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _troop,
                    _faction,
                    L.S("action_remove", "remove")
                ) == false
            )
                return; // Removal not allowed in current context

            InformationManager.ShowInquiry(
                new InquiryData(
                    titleText: L.S("remove_troop", "Remove Troop"),
                    text: L.T(
                            "remove_troop_text",
                            "Are you sure you want to permanently remove {TROOP_NAME}?\n\nTheir equipment will be stocked for later use, and existing troops will be converted to their {CULTURE} counterpart."
                        )
                        .SetTextVariable("TROOP_NAME", _troop.Name)
                        .SetTextVariable("CULTURE", _troop.Culture?.Name)
                        .ToString(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: () =>
                    {
                        TroopManager.Remove(_troop);

                        // Recreate the troop list
                        Editor.TroopList = new TroopListVM(_faction);
                    },
                    negativeAction: () => { }
                )
            );
        }

        /* ━━━━━━━━ Rank Up ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRankUp()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _troop,
                    _faction,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Rank up not allowed in current context

            int cost = TroopRules.RankUpCost(_troop);

            if (TroopRules.SkillPointsLeft(_troop) > 0)
            {
                Popup.Display(
                    L.T("rank_up_not_maxed_out", "Not Maxed Out"),
                    L.T(
                        "rank_up_not_maxed_out_text",
                        "Max out this retinue's skills before you can rank up."
                    )
                );
            }
            else if (Player.Gold < cost)
            {
                Popup.Display(
                    L.T("rank_up_not_enough_gold_title", "Not enough gold"),
                    L.T(
                            "rank_up_not_enough_gold_text",
                            "You do not have enough gold to rank up {TROOP_NAME}.\n\nRank up cost: {COST} gold."
                        )
                        .SetTextVariable("TROOP_NAME", _troop.Name)
                        .SetTextVariable("COST", cost)
                );
            }
            else if (TroopXpBehavior.Get(_troop) < cost && TroopXpIsEnabled)
            {
                Popup.Display(
                    L.T("rank_up_not_enough_xp_title", "Not enough XP"),
                    L.T(
                            "rank_up_not_enough_xp_text",
                            "You do not have enough XP to rank up {TROOP_NAME}.\n\nRank up cost: {COST} XP."
                        )
                        .SetTextVariable("TROOP_NAME", _troop.Name)
                        .SetTextVariable("COST", cost)
                );
            }
            else
            {
                string text = TroopXpIsEnabled
                    ? L.T(
                            "rank_up_costs_text",
                            "It will cost you {COST_GOLD} gold and {COST_XP} XP."
                        )
                        .SetTextVariable("COST_GOLD", cost)
                        .SetTextVariable("COST_XP", cost)
                        .ToString()
                    : L.T("rank_up_gold_cost_text", "It will cost you {COST} gold.")
                        .SetTextVariable("COST", cost)
                        .ToString();

                InformationManager.ShowInquiry(
                    new InquiryData(
                        titleText: L.S("rank_up", "Rank Up"),
                        text: L.T("increase_troop_tier", "Increase {TROOP_NAME}'s tier?\n\n{text}")
                            .SetTextVariable("TROOP_NAME", _troop.Name)
                            .SetTextVariable("text", text)
                            .ToString(),
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: true,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: () =>
                        {
                            TroopManager.RankUp(_troop);

                            // Refresh bindings
                            OnPropertyChanged(nameof(TierText));
                            OnPropertyChanged(nameof(SkillCapText));
                            OnPropertyChanged(nameof(SkillPointsTotal));
                            OnPropertyChanged(nameof(CanRankUp));
                        },
                        negativeAction: () => { }
                    )
                );
            }
        }

        /* ━━━━━━ Conversion ━━━━━━ */

        [DataSourceMethod]
        public void ExecuteClearConversions()
        {
            if (!HasPendingConversions)
                return;

            _staged.Clear();

            RefreshConversionStaging();
        }

        [DataSourceMethod]
        public void ExecuteApplyConversions()
        {
            if (!HasPendingConversions)
                return;

            if (
                TroopRules.IsAllowedInContextWithPopup(
                    _troop,
                    _faction,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            if (PendingTotalCost > Player.Gold)
            {
                Popup.Display(
                    L.T("convert_not_enough_gold_title", "Not enough gold"),
                    L.T(
                        "convert_not_enough_gold_text",
                        "You do not have enough gold to hire these retinues."
                    )
                );
                return;
            }

            foreach (var order in _staged.Values.ToList())
            {
                // Clamp to current max (no virtuals now; roster will change)
                int maxNow = TroopManager.GetMaxConvertible(order.Origin, order.Target);
                int amount = Math.Min(order.Amount, maxNow);
                if (amount <= 0)
                    continue;

                TroopManager.Convert(order.Origin, order.Target, amount, order.TotalCost);
            }

            _staged.Clear();

            RefreshConversionStaging();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Conversion Staging                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private string _stagedForTroopId;

        private readonly Dictionary<string, StagedConversion> _staged = [];

        private sealed class StagedConversion
        {
            public WCharacter Origin;
            public WCharacter Target;
            public int Amount;
            public int CostPer => TroopRules.ConversionCostPerUnit(Target);
            public int TotalCost => Amount * CostPer;
            public string Key => $"{Origin}:{Target}";
        }

        internal int GetStagedConversions(WCharacter from, WCharacter to)
        {
            string key = $"{from}->{to}";
            if (_staged.TryGetValue(key, out var order))
                return order.Amount;
            return 0;
        }

        internal int GetVirtualCount(WCharacter c)
        {
            int count = Player.Party.MemberRoster.CountOf(c);

            foreach (var o in _staged.Values)
            {
                if (o.Target == c)
                    count += o.Amount;
                if (o.Origin == c)
                    count -= o.Amount;
            }
            return count;
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
                int capLeft = Math.Max(0, cap - currentTo);
                availableFrom = Math.Min(availableFrom, capLeft);

                // also respect gold virtually
                int costPer = TroopRules.ConversionCostPerUnit(to);
                if (costPer > 0)
                {
                    int alreadyCost = PendingTotalCost;
                    int goldLeft = Math.Max(0, Player.Gold - alreadyCost);
                    int byGold = costPer > 0 ? goldLeft / costPer : availableFrom;
                    availableFrom = Math.Min(availableFrom, byGold);
                }
            }

            return Math.Max(0, availableFrom);
        }

        internal void StageConversion(WCharacter from, WCharacter to, int amountRequested)
        {
            if (
                to?.IsRetinue == true
                && !string.Equals(_stagedForTroopId, to.StringId, StringComparison.Ordinal)
            )
            {
                _staged.Clear();
                _stagedForTroopId = to.StringId;
            }

            int max = GetMaxStageable(from, to);
            int amount = Math.Min(amountRequested, max);
            if (amount <= 0)
                return;

            string key = $"{from}->{to}";
            string oppKey = $"{to}->{from}";

            // 1) Cancel against any opposite staged flow first.
            if (_staged.TryGetValue(oppKey, out var opp) && opp.Amount > 0)
            {
                int cancel = Math.Min(amount, opp.Amount);
                opp.Amount -= cancel;
                if (opp.Amount == 0)
                    _staged.Remove(oppKey);
                amount -= cancel;
            }

            // 2) Whatever remains is the net flow in this direction.
            if (amount > 0)
            {
                if (!_staged.TryGetValue(key, out var order))
                {
                    order = new StagedConversion
                    {
                        Origin = from,
                        Target = to,
                        Amount = 0,
                    };
                    _staged[key] = order;
                }
                order.Amount += amount;
            }
        }

        private void RefreshConversionStaging()
        {
            OnPropertyChanged(nameof(HasPendingConversions));
            OnPropertyChanged(nameof(PendingTotalCost));
            OnPropertyChanged(nameof(PendingTotalCount));
            OnPropertyChanged(nameof(TroopCount));

            foreach (var row in ConversionRows)
            {
                row.OnPropertyChanged(nameof(TroopConversionRowVM.OriginDisplay));
                row.OnPropertyChanged(nameof(TroopConversionRowVM.TargetDisplay));
                row.OnPropertyChanged(nameof(TroopConversionRowVM.PendingAmount));
                row.OnPropertyChanged(nameof(TroopConversionRowVM.CanRecruit));
                row.OnPropertyChanged(nameof(TroopConversionRowVM.CanRelease));
                row.OnPropertyChanged(nameof(TroopConversionRowVM.ConversionCost));
            }
        }
    }
}
