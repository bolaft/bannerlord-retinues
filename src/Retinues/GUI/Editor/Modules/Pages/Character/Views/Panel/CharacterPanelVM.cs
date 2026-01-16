using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Game.Experience;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Character.Controllers;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Views.Panel
{
    /// <summary>
    /// Character details panel.
    /// </summary>
    public class CharacterPanelVM : BasePanelVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public CharacterPanelVM()
        {
            RefreshSkillsGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool OnCharacterPage => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [EventListener(UIEvent.Name)]
        [DataSourceProperty]
        public string NameText => State.Character.Editable.Name;

        [DataSourceProperty]
        public Button<WCharacter> RenameButton { get; } =
            new(
                action: CharacterController.Rename,
                arg: () => State.Character,
                refresh: UIEvent.Name
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Culture & Race                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanChangeRace => CharacterController.CanChangeRace;

        [DataSourceProperty]
        public string CultureHeaderText =>
            CanChangeRace
                ? L.S("culture_header_text_with_race", "Culture & Species")
                : L.S("culture_header_text", "Culture");

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public string RaceText => CharacterController.GetRaceText();

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public string CultureText => State.Character.Editable.Culture?.Name;

        [DataSourceProperty]
        public Button<WCharacter> ChangeCultureButton { get; } =
            new(
                action: CharacterController.SelectCulture,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Culture],
                visibilityGate: () => !State.Character.IsRetinue,
                sprite: "SPClan.Parties.ChangePartyLeaderIcon",
                color: "f8eed1ff"
            );

        [DataSourceProperty]
        public Button<WCharacter> ChangeRaceButton { get; } =
            new(
                action: CharacterController.SelectRace,
                arg: () => State.Character.Editable as WCharacter,
                refresh: [UIEvent.Character, UIEvent.Culture],
                sprite: "SPClan.Parties.ChangePartyLeaderIcon",
                color: "f8eed1ff",
                visibilityGate: () => CharacterController.CanChangeRace
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Traits                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string TraitsHeaderText => L.S("traits_header_text", "Traits");

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowTraits => State.Character.IsHero;

        private MBBindingList<CharacterTraitVM> _traits;

        [DataSourceProperty]
        public MBBindingList<CharacterTraitVM> Traits
        {
            get
            {
                if (_traits == null)
                {
                    _traits = [];

                    foreach (var trait in WHero.PersonalityTraits)
                        _traits.Add(new CharacterTraitVM(trait));
                }
                return _traits;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string SkillsHeaderText => L.S("skills_header_text", "Skills");

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool ShowSkillDetails =>
            !State.Character.IsHero
            && (State.Mode == EditorMode.Player || Settings.EnforceSkillLimitsInUniversalMode);

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public string SkillTotalText =>
            $"{State.Character.SkillTotalUsed} / {State.Character.SkillTotal}";

        [EventListener(UIEvent.Skill)]
        [DataSourceProperty]
        public string SkillDescriptionText =>
            State.Mode == EditorMode.Player && Settings.EnableSkillGainSystem
                ? L.T(
                        "skill_description_text",
                        "Skill Points: {SKILL_POINTS} - Skill Cap: {SKILL_CAP} - Tier: {TIER}"
                    )
                    .SetTextVariable("SKILL_POINTS", State.Character.SkillPoints)
                    .SetTextVariable("SKILL_CAP", SkillRules.GetSkillCap(State.Character))
                    .SetTextVariable("TIER", Format.ToRoman(State.Character.Tier))
                    .ToString()
                : L.T("skill_description_text_short", "Skill Cap: {SKILL_CAP} - Tier: {TIER}")
                    .SetTextVariable("SKILL_CAP", SkillRules.GetSkillCap(State.Character))
                    .SetTextVariable("TIER", Format.ToRoman(State.Character.Tier))
                    .ToString();

        [DataSourceProperty]
        public Icon ExperienceIcon =>
            new(
                tooltip: new(
                    L.T(
                            "skill_experience_tooltip",
                            "{XP}/{XP_REQUIRED} XP towards next skill point."
                        )
                        .SetTextVariable("XP", State.Character.SkillPointsExperience)
                        .SetTextVariable("XP", State.Character.SkillPointsExperience)
                        .SetTextVariable(
                            "XP_REQUIRED",
                            SkillPointExperienceGain.GetXpRequiredForSkillPoint(
                                State.Character.Base
                            )
                        )
                ),
                refresh: [UIEvent.Character],
                visibilityGate: () =>
                    Settings.EnableSkillGainSystem && State.Mode == EditorMode.Player
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rank Up                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> RankUpButton { get; } =
            new(
                action: CharacterController.RankUp,
                arg: () => State.Character,
                refresh: [UIEvent.Skill],
                visibilityGate: () => State.Mode == EditorMode.Player && State.Character.IsRetinue
            );

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow1 { get; } = [];

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow2 { get; } = [];

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow3 { get; } = [];

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow4 { get; } = [];

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow5 { get; } = []; // optional safety

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow6 { get; } = []; // optional safety

        [DataSourceProperty]
        public int SkillsCount { get; private set; }

        [DataSourceProperty]
        public int SkillsGridRows { get; private set; }

        [DataSourceProperty]
        public int SkillsGridColumns { get; private set; }

        [DataSourceProperty]
        public bool SkillsLayout_2x4 { get; private set; }

        [DataSourceProperty]
        public bool SkillsLayout_3x4 { get; private set; }

        [DataSourceProperty]
        public bool SkillsLayout_3x6 { get; private set; }

        [DataSourceProperty]
        public bool SkillsLayout_4x6 { get; private set; }

        [DataSourceProperty]
        public bool SkillsLayout_Small { get; private set; }

        [EventListener(UIEvent.Character)]
        public void RefreshSkillsGrid()
        {
            var skills = State.Character.Skills;
            SkillsCount = skills.Count();

            var layout = SkillsGridLayout.ForCount(SkillsCount);
            SkillsGridRows = layout.Rows;
            SkillsGridColumns = layout.Columns;

            // Clear existing rows (keep list instances stable for Gauntlet)
            SkillsRow1.Clear();
            SkillsRow2.Clear();
            SkillsRow3.Clear();
            SkillsRow4.Clear();
            SkillsRow5.Clear();
            SkillsRow6.Clear();

            int i = 0;

            // Fill
            foreach (var (skill, value) in skills)
            {
                var r = i / layout.Columns;
                var vm = new CharacterSkillVM(skill);

                i++;

                switch (r)
                {
                    case 0:
                        SkillsRow1.Add(vm);
                        break;
                    case 1:
                        SkillsRow2.Add(vm);
                        break;
                    case 2:
                        SkillsRow3.Add(vm);
                        break;
                    case 3:
                        SkillsRow4.Add(vm);
                        break;
                    case 4:
                        SkillsRow5.Add(vm);
                        break;
                    case 5:
                        SkillsRow6.Add(vm);
                        break;
                    default:
                        break; // ignore extras or increase row count if you want
                }
            }

            // Notify
            OnPropertyChanged(nameof(SkillsCount));
            OnPropertyChanged(nameof(SkillsGridRows));
            OnPropertyChanged(nameof(SkillsGridColumns));
            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));
            OnPropertyChanged(nameof(SkillsRow3));
            OnPropertyChanged(nameof(SkillsRow4));
            OnPropertyChanged(nameof(SkillsRow5));
            OnPropertyChanged(nameof(SkillsRow6));

            // Layout flags
            SkillsLayout_2x4 = SkillsCount <= 8;
            SkillsLayout_3x4 = SkillsCount > 8 && SkillsCount <= 12;
            SkillsLayout_3x6 = SkillsCount > 12 && SkillsCount <= 18;
            SkillsLayout_4x6 = SkillsCount > 18 && SkillsCount <= 24;
            SkillsLayout_Small = SkillsCount > 24;

            // Notify layout flags
            OnPropertyChanged(nameof(SkillsLayout_2x4));
            OnPropertyChanged(nameof(SkillsLayout_3x4));
            OnPropertyChanged(nameof(SkillsLayout_3x6));
            OnPropertyChanged(nameof(SkillsLayout_4x6));
            OnPropertyChanged(nameof(SkillsLayout_Small));
        }

        public readonly struct SkillsGridLayout(
            int rows,
            int columns,
            int iconSize,
            int chevronSize,
            int smallMargin
        )
        {
            public readonly int Rows = rows;
            public readonly int Columns = columns;

            public readonly int IconSize = iconSize;
            public readonly int ChevronSize = chevronSize;
            public readonly int SmallMargin = smallMargin;

            public static SkillsGridLayout ForCount(int count)
            {
                // Preferred presets:
                // <= 8  -> 2x4
                // <= 12 -> 3x4
                // <= 18 -> 3x6
                // <= 24 -> 4x6
                // > 24  -> Nx6 (shrink gradually)

                if (count <= 8)
                    return new SkillsGridLayout(
                        rows: 2,
                        columns: 4,
                        iconSize: 80,
                        chevronSize: 20,
                        smallMargin: 4
                    );

                if (count <= 12)
                    return new SkillsGridLayout(
                        rows: 3,
                        columns: 4,
                        iconSize: 72,
                        chevronSize: 18,
                        smallMargin: 3
                    );

                if (count <= 18)
                    return new SkillsGridLayout(
                        rows: 3,
                        columns: 6,
                        iconSize: 64,
                        chevronSize: 16,
                        smallMargin: 2
                    );

                if (count <= 24)
                    return new SkillsGridLayout(
                        rows: 4,
                        columns: 6,
                        iconSize: 64,
                        chevronSize: 16,
                        smallMargin: 2
                    );

                // Fallback: keep 6 columns, add rows, shrink gently
                var rows = (int)Math.Ceiling(count / 6f);
                var extra = Math.Max(0, rows - 4);

                var w = Math.Max(46, 54 - extra * 4);
                var icon = w;
                var font = Math.Max(16, 18 - extra);
                var gapX = Math.Max(10, 16 - extra * 2);

                return new SkillsGridLayout(
                    rows: rows,
                    columns: 6,
                    iconSize: icon,
                    chevronSize: font,
                    smallMargin: gapX
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Upgrades                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<WCharacter> AddUpgradeTargetButton { get; } =
            new(
                action: CharacterTreeController.AddUpgradeTarget,
                arg: () => State.Character,
                refresh: [UIEvent.Character, UIEvent.Tree],
                label: L.S("add_upgrade_target_text", "Add Upgrade"),
                visibilityGate: () =>
                    CharacterTreeController.AddUpgradeTarget.Allow(State.Character)
            );

        private readonly MBBindingList<CharacterUpgradeVM> _upgradeSources = [];
        private readonly MBBindingList<CharacterUpgradeVM> _upgradeTargets = [];
        private bool _hasUpgradeSources;
        private bool _hasUpgradeTargets;

        [DataSourceProperty]
        public string UpgradeSourcesHeaderText => L.S("upgrade_sources_header_text", "Sources");

        [DataSourceProperty]
        public string UpgradeTargetsHeaderText => L.S("upgrade_targets_header_text", "Upgrades");

        [DataSourceProperty]
        public MBBindingList<CharacterUpgradeVM> UpgradeSources => _upgradeSources;

        [DataSourceProperty]
        public MBBindingList<CharacterUpgradeVM> UpgradeTargets => _upgradeTargets;

        [DataSourceProperty]
        public bool HasUpgradeSources => _hasUpgradeSources;

        [DataSourceProperty]
        public bool HasUpgradeTargets => _hasUpgradeTargets;

        [EventListener(UIEvent.Skill, UIEvent.Item)]
        private void RefreshUpgradesIfRetinue()
        {
            // Only retinues use conversion sources.
            if (State.Character?.IsRetinue == true)
                RefreshUpgrades();
        }

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        private void RefreshUpgrades()
        {
            _upgradeSources.Clear();
            _upgradeTargets.Clear();

            var c = State.Character;

            if (c != null)
            {
                // Use conversion sources for retinues, upgrade sources for regular troops.
                var sources = c.IsRetinue ? c.ConversionSources : c.UpgradeSources;

                foreach (var source in sources)
                    _upgradeSources.Add(new CharacterUpgradeVM(source));

                var targets = c.UpgradeTargets.ToList();

                // Ensure all player retinues that can convert from this troop are included.
                foreach (var retinue in WCharacter.GetPlayerRetinuesForSource(c))
                {
                    if (!targets.Contains(retinue))
                        targets.Add(retinue);
                }

                foreach (var target in targets)
                    _upgradeTargets.Add(new CharacterUpgradeVM(target));
            }

            var hasSources = _upgradeSources.Count > 0;
            var hasTargets = _upgradeTargets.Count > 0;

            if (hasSources != _hasUpgradeSources)
            {
                _hasUpgradeSources = hasSources;
                OnPropertyChanged(nameof(HasUpgradeSources));
            }

            if (hasTargets != _hasUpgradeTargets)
            {
                _hasUpgradeTargets = hasTargets;
                OnPropertyChanged(nameof(HasUpgradeTargets));
            }

            // The list instances stay stable, but their contents changed.
            // We still notify in case bindings rely on the property itself.
            OnPropertyChanged(nameof(UpgradeSources));
            OnPropertyChanged(nameof(UpgradeTargets));
        }
    }
}
