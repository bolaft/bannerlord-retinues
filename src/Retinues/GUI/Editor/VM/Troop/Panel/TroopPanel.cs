using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Xp.Behaviors;
using Retinues.Troops.Edition;
using Retinues.GUI.Helpers;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    [SafeClass]
    public sealed class TroopPanelVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static List<SkillObject> Row1Skills =>
            [
                DefaultSkills.Athletics,
                DefaultSkills.Riding,
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded,
            ];

        public static List<SkillObject> Row2Skills =>
            [
                DefaultSkills.Polearm,
                DefaultSkills.Bow,
                DefaultSkills.Crossbow,
                DefaultSkills.Throwing,
            ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] =
                [
                    nameof(ConversionRows),
                    nameof(SkillsRow1),
                    nameof(SkillsRow2),
                    nameof(Name),
                    nameof(GenderText),
                    nameof(TierText),
                    nameof(CanRankUp),
                    nameof(CanAddUpgrade),
                    nameof(TroopXpIsEnabled),
                    nameof(TroopXpText),
                    nameof(SkillCapText),
                    nameof(SkillPointsTotal),
                    nameof(SkillPointsUsed),
                    nameof(UpgradeTargets),
                    nameof(IsRetinue),
                    nameof(IsRegular),
                    nameof(HasPendingConversions),
                    nameof(PendingTotalCost),
                    nameof(PendingTotalCount),
                    nameof(RetinueCap),
                ],
                [UIEvent.Train] =
                [
                    nameof(SkillPointsUsed),
                    nameof(TrainingRequired),
                    nameof(TrainingRequiredText),
                    nameof(TrainingRequiredTextColor),
                ],
                [UIEvent.Conversion] =
                [
                    nameof(HasPendingConversions),
                    nameof(PendingTotalCost),
                    nameof(PendingTotalCount),
                ],
            };

        protected override void OnTroopChange() => Build();

        private void Build()
        {
            // Update conversion rows
            _conversionRows = State.Troop.IsRetinue
                ?
                [
                    .. TroopManager
                        .GetRetinueSourceTroops(State.Troop)
                        .Select(r => new TroopConversionRowVM(r)),
                ]
                : [];
            
            // Update visibility
            foreach (var row in ConversionRows)
                row.IsVisible = IsVisible;
            OnPropertyChanged(nameof(ConversionRows));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ Conversion Rows ━━━ */

        MBBindingList<TroopConversionRowVM> _conversionRows = [];

        [DataSourceProperty]
        public MBBindingList<TroopConversionRowVM> ConversionRows => _conversionRows;

        /* ━━━━━ Skills Row 1 ━━━━━ */

        readonly MBBindingList<TroopSkillVM> _skillsRow1 = [.. Row1Skills.Select(s => new TroopSkillVM(s))];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow1 => _skillsRow1;

        /* ━━━━━ Skills Row 2 ━━━━━ */

        readonly MBBindingList<TroopSkillVM> _skillsRow2 = [.. Row2Skills.Select(s => new TroopSkillVM(s))];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow2 => _skillsRow2;

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
        public string Name => Format.Crop(State.Troop?.Name, 35);

        [DataSourceProperty]
        public string GenderText =>
            State.Troop?.IsFemale == true ? L.S("female", "Female") : L.S("male", "Male");

        [DataSourceProperty]
        public string TierText
        {
            get
            {
                int tier = State.Troop?.Tier ?? 0;
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
        public bool CanRankUp => State.Troop?.IsRetinue == true && State.Troop?.IsMaxTier == false;

        /* ━━━━━━ Experience ━━━━━━ */

        [DataSourceProperty]
        public bool TroopXpIsEnabled =>
            Config.BaseSkillXpCost > 0 || Config.SkillXpCostPerPoint > 0;

        [DataSourceProperty]
        public string TroopXpText =>
            L.T("troop_xp", "{XP} xp")
                .SetTextVariable("XP", TroopXpBehavior.Get(State.Troop))
                .ToString();

        /* ━━━━━━━━ Skills ━━━━━━━━ */

        [DataSourceProperty]
        public string SkillCapText
        {
            get
            {
                int cap = State.Troop != null ? TroopRules.SkillCapByTier(State.Troop) : 0;
                return L.T("skill_cap_text", "{CAP} skill cap")
                    .SetTextVariable("CAP", cap)
                    .ToString();
            }
        }

        [DataSourceProperty]
        public int SkillPointsTotal =>
            State.Troop != null ? TroopRules.SkillTotalByTier(State.Troop) : 0;

        [DataSourceProperty]
        public int SkillPointsUsed => SkillsRow1.Concat(SkillsRow2).Sum(s => s.Value);

        /* ━━━━━━━ Training ━━━━━━━ */

        [DataSourceProperty]
        public int TrainingRequired => State.SkillData?.Sum(kv => kv.Value.Train?.Remaining) ?? 0;

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
        public bool IsRegular => State.Troop?.IsRetinue == false && State.Troop?.IsMilitia == false;

        [DataSourceProperty]
        public bool CanAddUpgrade => TroopRules.CanAddUpgradeToTroop(State.Troop);

        [DataSourceProperty]
        public string AddUpgradeButtonText => L.S("add_upgrade_button_text", "Add Upgrade");

        /* ━━━━━━ Conversion ━━━━━━ */

        [DataSourceProperty]
        public bool IsRetinue => State.Troop?.IsRetinue == true;

        [DataSourceProperty]
        public string ButtonApplyConversionsText => L.S("apply_conversions_button_text", "Convert");

        [DataSourceProperty]
        public string ButtonClearConversionsText => L.S("clear_conversions_button_text", "Clear");

        [DataSourceProperty]
        public bool HasPendingConversions => ConversionRows.Any(r => r.HasPendingConversions);

        [DataSourceProperty]
        public int PendingTotalCost => ConversionRows.Sum(r => r.ConversionCost);

        [DataSourceProperty]
        public int PendingTotalCount => ConversionRows.Sum(r => Math.Abs(r.PendingAmount));

        [DataSourceProperty]
        public int RetinueCap => State.Troop != null ? TroopRules.RetinueCapFor(State.Troop) : 0;

        /* ━━━━ Upgrade Targets ━━━ */

        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets =>
            [.. State.Troop.UpgradeTargets.Select(t => new TroopUpgradeTargetVM(t))];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Character ━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRename()
        {
            var oldName = State.Troop.Name;

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
                        State.Troop.Name = name.Trim();

                        // Fire event
                        State.UpdateTroop(State.Troop);
                    },
                    negativeAction: () => { },
                    defaultInputText: oldName
                )
            );
        }

        [DataSourceMethod]
        public void ExecuteChangeGender()
        {
            State.Troop.IsFemale = !State.Troop.IsFemale;

            State.UpdateTroop(State.Troop);
        }

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteAddUpgrade()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    State.Troop,
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

                        var target = TroopManager.AddUpgradeTarget(State.Troop, name);

                        // Refresh troops
                        State.UpdateFaction(State.Faction);

                        // Select the new troop
                        State.UpdateTroop(target);
                    },
                    negativeAction: () => { },
                    defaultInputText: State.Troop.Name
                )
            );
        }

        /* ━━━━━━━━ Rank Up ━━━━━━━ */

        [DataSourceMethod]
        public void ExecuteRankUp()
        {
            if (
                TroopRules.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Rank up not allowed in current context

            int cost = TroopRules.RankUpCost(State.Troop);

            if (TroopRules.SkillPointsLeft(State.Troop) > 0)
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
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .SetTextVariable("COST", cost)
                );
            }
            else if (TroopXpBehavior.Get(State.Troop) < cost && TroopXpIsEnabled)
            {
                Popup.Display(
                    L.T("rank_up_not_enough_xp_title", "Not enough XP"),
                    L.T(
                            "rank_up_not_enough_xp_text",
                            "You do not have enough XP to rank up {TROOP_NAME}.\n\nRank up cost: {COST} XP."
                        )
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
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
                            .SetTextVariable("TROOP_NAME", State.Troop.Name)
                            .SetTextVariable("text", text)
                            .ToString(),
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: true,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: () =>
                        {
                            TroopManager.RankUp(State.Troop);

                            // Refresh bindings
                            OnPropertyChanged(nameof(TierText));
                            OnPropertyChanged(nameof(SkillCapText));
                            OnPropertyChanged(nameof(SkillPointsTotal));
                            OnPropertyChanged(nameof(CanRankUp));

                            State.UpdateTroop(State.Troop);
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
            // Clear all pending conversions
            State.UpdateConversionData();
        }

        [DataSourceMethod]
        public void ExecuteApplyConversions()
        {
            if (!HasPendingConversions)
                return;

            if (
                TroopRules.IsAllowedInContextWithPopup(
                    State.Troop,
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

            foreach (var conversion in State.ConversionData)
            {
                Log.Info($"Processing conversion: {conversion.Key.Name} => {conversion.Value}");
                if (conversion.Value == 0)
                    continue;

                bool isRecruiting = conversion.Value > 0;

                var source = isRecruiting ? conversion.Key : State.Troop;
                var target = isRecruiting ? State.Troop : conversion.Key;

                int amount = Math.Abs(conversion.Value);

                TroopManager.Convert(source, target, amount);
            }

            // Clear pending conversions
            State.UpdateConversionData();

            // Refresh party data
            State.UpdatePartyData();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void Show()
        {
            base.Show();

            Build();

            foreach (var row in ConversionRows)
                row.Show();

            foreach (var skill in SkillsRow1.Concat(SkillsRow2))
                skill.Show();
        }

        public override void Hide()
        {
            foreach (var row in ConversionRows)
                row.Hide();
            foreach (var skill in SkillsRow1.Concat(SkillsRow2))
                skill.Hide();

            base.Hide();
        }
    }
}
