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
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    [SafeClass]
    public sealed class TroopPanelVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly TroopScreenVM Screen;

        public TroopPanelVM(TroopScreenVM screen)
        {
            Log.Info("Building TroopPanelVM...");

            Screen = screen;
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopPanelVM...");

            // Components
            foreach (var row in ConversionRows)
                row.Initialize();

            foreach (var skill in SkillsRow1)
                skill.Initialize();

            foreach (var skill in SkillsRow2)
                skill.Initialize();

            // Subscribe to events
            EventManager.TroopChange.Register(() =>
            {
                RebuildForSelectedTroop();
                Raise(
                    nameof(Name),
                    nameof(GenderText),
                    nameof(TierText),
                    nameof(TroopXpIsEnabled),
                    nameof(TroopXpText),
                    nameof(SkillsRow1),
                    nameof(SkillsRow2),
                    nameof(SkillCapText),
                    nameof(SkillPointsTotal),
                    nameof(SkillPointsUsed),
                    nameof(CanAddUpgrade),
                    nameof(UpgradeTargets),
                    nameof(ConversionRows),
                    nameof(HasPendingConversions),
                    nameof(PendingTotalCost),
                    nameof(PendingTotalCount),
                    nameof(TroopCount),
                    nameof(RetinueCap)
                );
            });

            EventManager.ConversionChange.Register(() =>
            {
                OnPropertyChanged(nameof(ConversionRows));
                Raise(nameof(HasPendingConversions),
                    nameof(PendingTotalCost),
                    nameof(PendingTotalCount),
                    nameof(TroopCount)
                );
            });

            EventManager.SkillChange.RegisterProperties(
                this,
                nameof(SkillsRow1),
                nameof(SkillsRow2),
                nameof(SkillCapText),
                nameof(SkillPointsTotal),
                nameof(SkillPointsUsed),
                nameof(CanRankUp),
                nameof(TrainingRequiredText),
                nameof(TrainingRequiredTextColor)
            );

            EventManager.NameChange.RegisterProperties(this, nameof(Name));
            EventManager.TierChange.RegisterProperties(this, nameof(TierText));
            EventManager.GenderChange.RegisterProperties(this, nameof(GenderText));

            RebuildForSelectedTroop();
        }

        private void RebuildForSelectedTroop()
        {
            // Skills
            _skillsRow1.Clear();
            _skillsRow2.Clear();

            var troop = SelectedTroop;
            if (troop != null)
            {
                foreach (var s in troop.Skills.Take(4))
                {
                    var vm = new TroopSkillVM(Screen, s.Key);
                    vm.Initialize();
                    _skillsRow1.Add(vm);
                }
                foreach (var s in troop.Skills.Skip(4).Take(4))
                {
                    var vm = new TroopSkillVM(Screen, s.Key);
                    vm.Initialize();
                    _skillsRow2.Add(vm);
                }
            }
            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));

            // Upgrade targets
            _upgradeTargets.Clear();
            foreach (var target in troop?.UpgradeTargets ?? Enumerable.Empty<WCharacter>())
                _upgradeTargets.Add(new TroopUpgradeTargetVM(target));
            OnPropertyChanged(nameof(UpgradeTargets));

            // Conversions
            _conversionRows.Clear();
            if (troop?.IsRetinue == true)
            {
                foreach (var src in TroopManager.GetRetinueSourceTroops(troop))
                {
                    var row = new TroopConversionRowVM(Screen, src);
                    row.Initialize();
                    _conversionRows.Add(row);
                }
            }
            OnPropertyChanged(nameof(ConversionRows));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WCharacter SelectedTroop => Screen?.TroopList?.Selection?.Troop;

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

        /* ━━━━━━━ Character ━━━━━━ */

        [DataSourceProperty]
        public string Name => Format.Crop(SelectedTroop?.Name, 35);

        [DataSourceProperty]
        public string GenderText =>
            SelectedTroop?.IsFemale == true ? L.S("female", "Female") : L.S("male", "Male");

        [DataSourceProperty]
        public string TierText
        {
            get
            {
                int tier = SelectedTroop?.Tier ?? 0;
                string roman = tier switch
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
                    10 => "X",
                    _ => tier.ToString(),
                };
                return $"{L.S("tier", "Tier")} {roman}";
            }
        }

        /* ━━━━━━━━ Rank Up ━━━━━━━ */

        [DataSourceProperty]
        public bool CanRankUp =>
            SelectedTroop?.IsRetinue == true && SelectedTroop?.IsMaxTier == false;

        /* ━━━━━━ Experience ━━━━━━ */

        [DataSourceProperty]
        public bool TroopXpIsEnabled =>
            Config.BaseSkillXpCost > 0 || Config.SkillXpCostPerPoint > 0;

        [DataSourceProperty]
        public string TroopXpText =>
            L.T("troop_xp", "{XP} xp")
                .SetTextVariable("XP", TroopXpBehavior.Get(SelectedTroop))
                .ToString();

        /* ━━━━━━━━ Skills ━━━━━━━━ */

        private readonly MBBindingList<TroopSkillVM> _skillsRow1 = [];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow1 => _skillsRow1;

        private readonly MBBindingList<TroopSkillVM> _skillsRow2 = [];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow2 => _skillsRow2;

        [DataSourceProperty]
        public string SkillCapText
        {
            get
            {
                int cap = TroopRules.SkillCapByTier(SelectedTroop);
                return L.T("skill_cap_text", "{CAP} skill cap")
                    .SetTextVariable("CAP", cap)
                    .ToString();
            }
        }

        [DataSourceProperty]
        public int SkillPointsTotal => TroopRules.SkillTotalByTier(SelectedTroop);

        [DataSourceProperty]
        public int SkillPointsUsed =>
            (SelectedTroop?.Skills?.Values.Sum() ?? 0)
            + TroopTrainBehavior
                .Instance.GetStagedChanges(SelectedTroop)
                .Sum(d => d.PointsRemaining);

        /* ━━━━━━━ Training ━━━━━━━ */

        // Helper
        private int TrainingRequired =>
            TroopTrainBehavior.Instance.GetStagedChanges(SelectedTroop).Sum(data => data.Remaining);

        [DataSourceProperty]
        public bool TrainingTakesTime => Config.TrainingTakesTime;

        [DataSourceProperty]
        public string TrainingRequiredText
        {
            get
            {
                if (TrainingRequired == 0)
                    return L.S("no_training_required", "No training required");

                return L.T("training_required", "{HOURS} hours of training required")
                    .SetTextVariable("HOURS", TrainingRequired)
                    .ToString();
            }
        }

        [DataSourceProperty]
        public string TrainingRequiredTextColor => TrainingRequired > 0 ? "#ebaf2fff" : "#F4E1C4FF";

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceProperty]
        public bool CanAddUpgrade => TroopRules.CanAddUpgradeToTroop(SelectedTroop);

        [DataSourceProperty]
        public string AddUpgradeButtonText => L.S("add_upgrade_button_text", "Add Upgrade");
        private readonly MBBindingList<TroopUpgradeTargetVM> _upgradeTargets = [];

        [DataSourceProperty]
        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets => _upgradeTargets;

        /* ━━━━━━ Conversion ━━━━━━ */

        [DataSourceProperty]
        public bool IsRetinue => SelectedTroop?.IsRetinue == true;

        private readonly MBBindingList<TroopConversionRowVM> _conversionRows = [];

        [DataSourceProperty]
        public MBBindingList<TroopConversionRowVM> ConversionRows => _conversionRows;

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
        public int TroopCount => GetVirtualCount(SelectedTroop);

        [DataSourceProperty]
        public int RetinueCap => TroopRules.RetinueCapFor(SelectedTroop);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Character ━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRename()
        {
            var oldName = SelectedTroop.Name;

            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    titleText: L.S("rename_troop", "Rename Troop"),
                    text: L.S("enter_new_name", "Enter a new name:"),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: true,
                    affirmativeText: L.S("confirm", "Confirm"),
                    negativeText: L.S("cancel", "Cancel"),
                    affirmativeAction: name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            return;

                        // Apply the new name
                        SelectedTroop.Name = name.Trim();

                        EventManager.NameChange.Fire();
                    },
                    negativeAction: () => { },
                    defaultInputText: oldName
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            SelectedTroop.IsFemale = !SelectedTroop.IsFemale;

            EventManager.GenderChange.Fire();
        }

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteAddUpgrade()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    SelectedTroop,
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

                        var target = TroopManager.AddUpgradeTarget(SelectedTroop, name);

                        // Select the new troop
                        Screen.TroopList.Select(target);
                    },
                    negativeAction: () => { },
                    defaultInputText: SelectedTroop.Name
                )
            );
        }

        /* ━━━━━━━━ Rank Up ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRankUp()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    SelectedTroop,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Rank up not allowed in current context

            int cost = TroopRules.RankUpCost(SelectedTroop);

            if (TroopRules.SkillPointsLeft(SelectedTroop) > 0)
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
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                        .SetTextVariable("COST", cost)
                );
            }
            else if (TroopXpBehavior.Get(SelectedTroop) < cost && TroopXpIsEnabled)
            {
                Popup.Display(
                    L.T("rank_up_not_enough_xp_title", "Not enough XP"),
                    L.T(
                            "rank_up_not_enough_xp_text",
                            "You do not have enough XP to rank up {TROOP_NAME}.\n\nRank up cost: {COST} XP."
                        )
                        .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
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
                            .SetTextVariable("TROOP_NAME", SelectedTroop.Name)
                            .SetTextVariable("text", text)
                            .ToString(),
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: true,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: () =>
                        {
                            TroopManager.RankUp(SelectedTroop);

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

            EventManager.ConversionChange.Fire();
        }

        [DataSourceMethod]
        public void ExecuteApplyConversions()
        {
            if (!HasPendingConversions)
                return;

            if (
                TroopRules.IsAllowedInContextWithPopup(
                    SelectedTroop,
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

            EventManager.ConversionChange.Fire();
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
            public int TotalCost => Amount * TroopRules.ConversionCostPerUnit(Target);
        }

        internal int GetStagedConversions(WCharacter from, WCharacter to)
        {
            string key = $"{from}:{to}";
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

            string key = $"{from}:{to}";
            string oppKey = $"{to}:{from}";

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

            EventManager.ConversionChange.Fire();
        }
    }
}
