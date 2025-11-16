using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Experience;
using Retinues.Game;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel for the troop details panel, exposing skills, upgrades and conversions.
    /// </summary>
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
                    nameof(AddUpgradeHint),
                    nameof(TroopXpIsEnabled),
                    nameof(TroopXpText),
                    nameof(SkillCapText),
                    nameof(SkillPointsTotal),
                    nameof(SkillPointsUsed),
                    nameof(UpgradeTargets),
                    nameof(IsRetinue),
                    nameof(IsRegular),
                    nameof(ShowUpgradesHeader),
                    nameof(ShowSkillSummary),
                    nameof(IsCustomRegular),
                    nameof(HasPendingConversions),
                    nameof(PendingTotalGoldCost),
                    nameof(PendingTotalInfluenceCost),
                    nameof(PendingTotalCount),
                    nameof(RetinueCap),
                    nameof(CultureText),
                    nameof(RaceText),
                    nameof(CanChangeRace),
                    nameof(FormationClassIcon),
                    nameof(FormationClassText),
                ],
                [UIEvent.Train] =
                [
                    nameof(SkillPointsUsed),
                    nameof(TroopXpText),
                    nameof(TrainingRequired),
                    nameof(TrainingRequiredText),
                    nameof(TrainingRequiredTextColor),
                    nameof(TrainingRequiredHint),
                ],
                [UIEvent.Conversion] =
                [
                    nameof(HasPendingConversions),
                    nameof(PendingTotalGoldCost),
                    nameof(PendingTotalInfluenceCost),
                    nameof(PendingTotalCount),
                ],
            };

        private bool _needsRebuild = true;

        protected override void OnTroopChange()
        {
            _needsRebuild = true;
            if (IsVisible)
                Build();
        }

        protected override void OnConversionChange()
        {
            _needsRebuild = true;
            if (IsVisible)
                Build();
        }

        /// <summary>
        /// Rebuild internal lists (conversion rows) and update visibility.
        /// </summary>
        private void Build()
        {
            if (_needsRebuild)
            {
                _needsRebuild = false;

                if (State.Troop.IsRetinue)
                {
                    IEnumerable<WCharacter> data =
                        State.ConversionData?.Keys ?? Enumerable.Empty<WCharacter>();

                    _conversionRows =
                    [
                        .. data.Select(conversion => new TroopConversionRowVM(conversion)),
                    ];
                }
                else
                {
                    _conversionRows = [];
                }

                OnPropertyChanged(nameof(ConversionRows));
            }

            // Update visibility
            foreach (var row in ConversionRows)
                row.IsVisible = IsVisible;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ Conversion Rows ━━━ */

        MBBindingList<TroopConversionRowVM> _conversionRows = [];

        [DataSourceProperty]
        public MBBindingList<TroopConversionRowVM> ConversionRows => _conversionRows;

        /* ━━━━━ Skills Row 1 ━━━━━ */

        readonly MBBindingList<TroopSkillVM> _skillsRow1 =
        [
            .. Row1Skills.Select(s => new TroopSkillVM(s)),
        ];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow1 => _skillsRow1;

        /* ━━━━━ Skills Row 2 ━━━━━ */

        readonly MBBindingList<TroopSkillVM> _skillsRow2 =
        [
            .. Row2Skills.Select(s => new TroopSkillVM(s)),
        ];

        [DataSourceProperty]
        public MBBindingList<TroopSkillVM> SkillsRow2 => _skillsRow2;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Headers ━━━━━━━ */

        [DataSourceProperty]
        public string CultureHeaderText =>
            CanChangeRace
                ? L.S("culture_and_race_header_text", "Culture & Race")
                : L.S("culture_header_text", "Culture");

        [DataSourceProperty]
        public string RaceHeaderText => L.S("race_header_text", "Race");

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [DataSourceProperty]
        public string SkillsHeaderText => L.S("skills_header_text", "Skills & Formation");

        [DataSourceProperty]
        public string FormationClassHeaderText =>
            L.S("formation_class_header_text", "Formation Class");

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
        public string CultureText => State.Troop?.Culture?.Name ?? L.S("unknown", "Unknown");

        [DataSourceProperty]
        public string RaceText => GetRaceName(State.Troop?.Race ?? -1) ?? L.S("unknown", "Unknown");

        [DataSourceProperty]
        public bool CanChangeRace => FaceGen.GetRaceCount() > 1;

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
            !ClanScreen.IsStudioMode
            && (Config.BaseSkillXpCost > 0 || Config.SkillXpCostPerPoint > 0);

        [DataSourceProperty]
        public string TroopXpText =>
            L.T("troop_xp", "{XP} xp")
                .SetTextVariable("XP", BattleXpBehavior.Get(State.Troop))
                .ToString();

        /* ━━━━━━━━ Skills ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowSkillSummary => State.Troop?.IsHero == false;

        [DataSourceProperty]
        public string SkillCapText
        {
            get
            {
                int cap = State.Troop != null ? SkillManager.SkillCapByTier(State.Troop) : 0;
                return L.T("skill_cap_text", "{CAP} skill cap")
                    .SetTextVariable("CAP", cap)
                    .ToString();
            }
        }

        [DataSourceProperty]
        public int SkillPointsTotal =>
            State.Troop != null ? SkillManager.SkillTotalByTier(State.Troop) : 0;

        [DataSourceProperty]
        public int SkillPointsUsed => SkillsRow1.Concat(SkillsRow2).Sum(s => s.Value);

        /* ━━━━━━━ Training ━━━━━━━ */

        [DataSourceProperty]
        public int TrainingRequired => State.SkillData?.Sum(kv => kv.Value.Train?.Remaining) ?? 0;

        [DataSourceProperty]
        public bool TrainingTakesTime => Config.TrainingTakesTime && !ClanScreen.IsStudioMode;

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

        [DataSourceProperty]
        public BasicTooltipViewModel TrainingRequiredHint =>
            Tooltip.MakeTooltip(
                null,
                L.S(
                    "training_required_tooltip_body",
                    "Before skill point increases are applied, troops must undergo training.\n\nThis is done by selecting 'Train troops' from a fief's town menu."
                )
            );

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        [DataSourceProperty]
        public bool IsRegular => State.Troop?.IsRetinue == false && State.Troop?.IsMilitia == false;

        [DataSourceProperty]
        public bool ShowUpgradesHeader =>
            IsRegular && (!ClanScreen.IsStudioMode || UpgradeTargets.Count > 0);

        [DataSourceProperty]
        public bool IsCustomRegular
        {
            get
            {
                bool isRegular = IsRegular;
                bool isCustom = State.Troop?.IsCustom == true;

                return isRegular && isCustom;
            }
        }

        [DataSourceProperty]
        public bool CanAddUpgrade => CantAddUpgradeReason == null;

        [DataSourceProperty]
        public string AddUpgradeButtonText => L.S("add_upgrade_button_text", "Add Upgrade");

        [DataSourceProperty]
        public TextObject CantAddUpgradeReason =>
            UpgradeManager.GetAddUpgradeToTroopReason(State.Troop);

        [DataSourceProperty]
        public BasicTooltipViewModel AddUpgradeHint =>
            CanAddUpgrade ? null : Tooltip.MakeTooltip(null, CantAddUpgradeReason.ToString());

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
        public int PendingTotalGoldCost => ConversionRows.Sum(r => r.GoldConversionCost);

        [DataSourceProperty]
        public int PendingTotalInfluenceCost => ConversionRows.Sum(r => r.InfluenceConversionCost);

        [DataSourceProperty]
        public int PendingTotalCount => ConversionRows.Sum(r => Math.Abs(r.PendingAmount));

        [DataSourceProperty]
        public int RetinueCap =>
            State.Troop != null ? RetinueManager.RetinueCapFor(State.Troop) : 0;

        /* ━━━━ Upgrade Targets ━━━ */

        public MBBindingList<TroopUpgradeTargetVM> UpgradeTargets =>
            [.. State.Troop.UpgradeTargets.Select(t => new TroopUpgradeTargetVM(t))];

        /* ━━━━ Formation Class ━━━ */

        [DataSourceProperty]
        public string FormationClassText =>
            State.Troop?.FormationClass.GetLocalizedName().ToString();

        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(State.Troop);

        [DataSourceProperty]
        public BasicTooltipViewModel FormationClassHint =>
            Tooltip.MakeTooltip(null, L.S("formation_class_hint", "Change Formation Class"));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Character ━━━━━━ */

        /// <summary>
        /// Prompt to rename the selected troop.
        /// </summary>
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

        /// <summary>
        /// Change the selected troop's culture.
        /// </summary>[DataSourceMethod]
        [DataSourceMethod]
        public void ExecuteChangeCulture()
        {
            try
            {
                if (State.Troop == null)
                    return;

                List<InquiryElement> elements = [];

                // Build selection elements (single-select).
                foreach (var wc in WCulture.All)
                {
                    if (wc?.Name == null)
                        continue;

                    var root = wc.RootBasic ?? wc.RootElite;
                    var imageIdentifier = root?.ImageIdentifier;

                    // Mods (shokuho) can have weird behaviors, skip them.
                    if (imageIdentifier == null)
                        continue;

                    elements.Add(
                        new InquiryElement(wc.Base, wc.Name, root.ImageIdentifier, true, null)
                    );
                }

                if (elements.Count == 0)
                {
                    Notifications.Popup(
                        L.T("no_cultures_title", "No Cultures Found"),
                        L.T("no_cultures_text", "No cultures are loaded in the current game.")
                    );
                    return;
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("change_culture_title", "Change Culture"),
                        descriptionText: null,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: selected =>
                        {
                            if (selected == null || selected.Count == 0)
                                return;
                            if (selected[0]?.Identifier is not CultureObject culture)
                                return;
                            if (culture.StringId == State.Troop.Culture.StringId)
                                return; // No change

                            // Store prior race before culture change.
                            var priorRace = State.Troop.Race;

                            // Apply via wrapper (uses reflection under the hood).
                            State.Troop.Culture = new WCulture(culture);

                            // Update visuals.
                            BodyHelper.ApplyPropertiesFromCulture(State.Troop, culture);

                            // Reapply prior race if needed.
                            if (priorRace >= 0)
                            {
                                State.Troop.Race = priorRace;
                                State.Troop.Body.EnsureOwnBodyRange();
                            }

                            // Refresh VM bindings & visuals.
                            State.UpdateTroop(State.Troop);
                        },
                        negativeAction: new Action<List<InquiryElement>>(_ => { })
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Change the selected troop's race.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteChangeRace()
        {
            try
            {
                if (State.Troop == null || !CanChangeRace)
                    return;

                var names = EnsureRaceNames();
                if (names == null || names.Count == 0)
                {
                    Notifications.Popup(
                        L.T("no_races_title", "No Races Found"),
                        L.T("no_races_text", "No alternative races are available.")
                    );
                    return;
                }

                var elements = new List<InquiryElement>(names.Count);
                for (int i = 0; i < names.Count; i++)
                {
                    var name = string.IsNullOrWhiteSpace(names[i]) ? $"Race {i}" : names[i];
                    elements.Add(new InquiryElement(i, name, null, true, null));
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("change_race_title", "Change Race"),
                        descriptionText: L.S("change_race_desc", string.Empty),
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: selected =>
                        {
                            if (selected?.Count > 0 && selected[0]?.Identifier is int race)
                            {
                                State.Troop.Race = race;
                                State.Troop.Body.EnsureOwnBodyRange();
                                State.UpdateTroop(State.Troop);
                            }
                        },
                        negativeAction: _ => { }
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Change the selected troop's formation class override setting.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteChangeFormationClass()
        {
            try
            {
                if (State.Troop == null)
                    return;

                List<FormationClass> classes =
                [
                    FormationClass.Unset,
                    FormationClass.Infantry,
                    FormationClass.Ranged,
                    FormationClass.Cavalry,
                    FormationClass.HorseArcher,
                ];

                var elements = new List<InquiryElement>(classes.Count);

                foreach (var c in classes)
                {
                    string text =
                        c == FormationClass.Unset
                            ? L.S("formation_auto", "Auto")
                            : c.GetLocalizedName().ToString();

                    elements.Add(new InquiryElement(c, text, null, true, null));
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new MultiSelectionInquiryData(
                        titleText: L.S("change_formation_class_title", "Change Formation Class"),
                        descriptionText: null,
                        inquiryElements: elements,
                        isExitShown: true,
                        minSelectableOptionCount: 1,
                        maxSelectableOptionCount: 1,
                        affirmativeText: L.S("confirm", "Confirm"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: selected =>
                        {
                            if (selected == null || selected.Count == 0)
                                return;
                            if (selected[0]?.Identifier is not FormationClass formationClass)
                                return;
                            if (formationClass == State.Troop.FormationClassOverride)
                                return; // No change

                            // Set override
                            State.Troop.FormationClassOverride = formationClass;

                            // Update formation class if needed
                            State.Troop.FormationClass = State.Troop.ComputeFormationClass();

                            // Refresh VM bindings
                            State.UpdateTroop(State.Troop);
                        },
                        negativeAction: new Action<List<InquiryElement>>(_ => { })
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /* ━━━━━━━ Upgrades ━━━━━━━ */

        /// <summary>
        /// Prompt to add a new upgrade target for the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteAddUpgrade()
        {
            if (
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Modification not allowed in current context

            if (ClanScreen.IsStudioMode)
                return; // Adding upgrades disabled in studio mode

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

                        var target = UpgradeManager.AddUpgradeTarget(State.Troop, name);

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

        /// <summary>
        /// Attempt to rank up the selected troop with confirmations.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRankUp()
        {
            if (ClanScreen.IsStudioMode)
                return; // Rank up disabled in studio mode

            if (
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_modify", "modify")
                ) == false
            )
                return; // Rank up not allowed in current context

            int cost = RetinueManager.RankUpCost(State.Troop);

            if (SkillManager.SkillPointsLeft(State.Troop) > 0)
            {
                Notifications.Popup(
                    L.T("rank_up_not_maxed_out", "Not Maxed Out"),
                    L.T(
                        "rank_up_not_maxed_out_text",
                        "Max out this retinue's skills before you can rank up."
                    )
                );
            }
            else if (
                State.Troop == State.Faction.RetinueBasic
                && State.Troop.Tier >= State.Faction.RetinueElite.Tier
            )
            {
                Notifications.Popup(
                    L.T("rank_up_cant_outrank_elite_title", "Cannot Outrank Elite"),
                    L.T(
                            "rank_up_cant_outrank_elite_text",
                            "{TROOP_NAME} can't outrank {ELITE_RETINUE}."
                        )
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .SetTextVariable("ELITE_RETINUE", State.Faction.RetinueElite.Name)
                );
            }
            else if (Player.Gold < cost)
            {
                Notifications.Popup(
                    L.T("rank_up_not_enough_gold_title", "Not enough gold"),
                    L.T(
                            "rank_up_not_enough_gold_text",
                            "You do not have enough gold to rank up {TROOP_NAME}.\n\nRank up cost: {COST} gold."
                        )
                        .SetTextVariable("TROOP_NAME", State.Troop.Name)
                        .SetTextVariable("COST", cost)
                );
            }
            else if (BattleXpBehavior.Get(State.Troop) < cost && TroopXpIsEnabled)
            {
                Notifications.Popup(
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
                            RetinueManager.RankUp(State.Troop);

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

        /// <summary>
        /// Apply all staged conversions (with checks and cost handling).
        /// </summary>
        [DataSourceMethod]
        public void ExecuteApplyConversions()
        {
            if (!HasPendingConversions)
                return;

            if (
                ContextManager.IsAllowedInContextWithPopup(
                    State.Troop,
                    L.S("action_convert", "convert")
                ) == false
            )
                return; // Conversion not allowed in current context

            if (PendingTotalGoldCost > Player.Gold)
            {
                Notifications.Popup(
                    L.T("convert_not_enough_gold_title", "Not enough gold"),
                    L.T(
                        "convert_not_enough_gold_text",
                        "You do not have enough gold to hire these retinues."
                    )
                );
                return;
            }

            if (PendingTotalInfluenceCost > Player.Influence)
            {
                Notifications.Popup(
                    L.T("convert_not_enough_influence_title", "Not enough influence"),
                    L.T(
                        "convert_not_enough_influence_text",
                        "You do not have enough influence to hire these retinues."
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

                RetinueManager.Convert(source, target, amount);
            }

            // Clear pending conversions
            State.ClearPendingConversions();

            // Refresh party data
            State.UpdatePartyData();
        }

        /// <summary>
        /// Clear all staged conversion selections.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteClearConversions()
        {
            // Clear all pending conversions
            State.ClearPendingConversions();
        }

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        private static List<string> _cachedRaceNames;

        private static string GetRaceName(int raceIndex)
        {
            if (raceIndex < 0)
                return L.S("unknown", "Unknown");

            var names = EnsureRaceNames();
            if (names != null && raceIndex < names.Count)
                return names[raceIndex] ?? $"Race {raceIndex}";

            return $"Race {raceIndex}";
        }

        private static IReadOnlyList<string> EnsureRaceNames()
        {
            if (_cachedRaceNames != null)
                return _cachedRaceNames;

            var raw = FaceGen.GetRaceNames() ?? Array.Empty<string>();
            if (raw.Length == 0)
                return _cachedRaceNames = new List<string>();

            var list = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
                list.Add(FormatRaceName(raw[i]));

            _cachedRaceNames = list;

            return _cachedRaceNames;
        }

        private static string FormatRaceName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var parts = raw.Replace('_', ' ')
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.ToLowerInvariant())
                .ToArray();

            if (parts.Length == 0)
                return null;

            string candidate = string.Join(" ", parts.Take(2));

            if (candidate.Length > 15)
            {
                candidate = candidate.Substring(0, 15);
                int lastSpace = candidate.LastIndexOf(' ');
                if (lastSpace > 0)
                    candidate = candidate.Substring(0, lastSpace);
            }

            if (string.IsNullOrWhiteSpace(candidate))
                candidate = parts[0];

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(candidate);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Show this panel and its child components.
        /// </summary>
        public override void Show()
        {
            base.Show();

            if (_needsRebuild)
                Build();

            foreach (var row in ConversionRows)
                row.Show();

            foreach (var skill in SkillsRow1.Concat(SkillsRow2))
                skill.Show();
        }

        /// <summary>
        /// Hide this panel and its child components.
        /// </summary>
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
