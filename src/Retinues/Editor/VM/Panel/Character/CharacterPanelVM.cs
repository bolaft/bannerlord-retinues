using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Character
{
    /// <summary>
    /// Character details panel.
    /// </summary>
    public class CharacterPanelVM : BaseVM
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

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [EventListener(UIEvent.Character, UIEvent.Name)]
        [DataSourceProperty]
        public string NameText => State.Character.Editable.Name;

        [DataSourceMethod]
        public void ExecuteRename() => CharacterController.ChangeName();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Culture & Race                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool CanChangeRace => CharacterController.CanChangeRace;

        [DataSourceProperty]
        public string CultureHeaderText =>
            CanChangeRace
                ? L.S("culture_header_text_with_race", "Culture & Race")
                : L.S("culture_header_text", "Culture");

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public string RaceText => CharacterController.GetRaceText();

        [EventListener(UIEvent.Culture)]
        [DataSourceProperty]
        public string CultureText => State.Character.Editable.Culture?.Name;

        [DataSourceMethod]
        public void ExecuteChangeCulture()
        {
            var elements = new List<InquiryElement>();

            foreach (var culture in WCulture.All)
            {
                var imageIdentifier = culture.ImageIdentifier;
                var name = culture.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: culture,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_cultures_title", "No Cultures Found"),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("change_culture_title", "Change Culture"),
                elements: elements,
                onSelect: element =>
                    CharacterController.ChangeCulture(element?.Identifier as WCulture)
            );
        }

        [DataSourceMethod]
        public void ExecuteChangeRace() => CharacterController.OpenRaceSelector();

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
            var skills =
                Skills.GetSkillListForCharacter(State.Character.IsHero, includeModded: true) ?? [];
            var list = skills.Where(s => s != null).Distinct().ToList();

            SkillsCount = list.Count;

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

            // Fill
            for (int i = 0; i < list.Count; i++)
            {
                var r = i / layout.Columns;
                var vm = new CharacterSkillVM(list[i]);

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
                var h = Math.Max(72, 84 - extra * 6);
                var icon = w;
                var font = Math.Max(16, 18 - extra);
                var gapX = Math.Max(10, 16 - extra * 2);
                var gapY = Math.Max(8, 10 - extra);

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

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        [DataSourceProperty]
        public bool HasUpgradeSources => State.Character.UpgradeSources.Any();

        [DataSourceProperty]
        public string UpgradeSourcesHeaderText => L.S("upgrade_sources_header_text", "Origin");

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        [DataSourceProperty]
        public MBBindingList<CharacterUpgradeVM> UpgradeSources
        {
            get
            {
                var sources = new MBBindingList<CharacterUpgradeVM>();
                foreach (var source in State.Character.UpgradeSources)
                    sources.Add(new CharacterUpgradeVM(source));
                return sources;
            }
        }

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        [DataSourceProperty]
        public bool HasUpgradeTargets => State.Character.UpgradeTargets.Any();

        [DataSourceProperty]
        public string UpgradeTargetsHeaderText => L.S("upgrade_targets_header_text", "Upgrades");

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        [DataSourceProperty]
        public MBBindingList<CharacterUpgradeVM> UpgradeTargets
        {
            get
            {
                var targets = new MBBindingList<CharacterUpgradeVM>();
                foreach (var source in State.Character.UpgradeTargets)
                    targets.Add(new CharacterUpgradeVM(source));
                return targets;
            }
        }

        [EventListener(UIEvent.Character, UIEvent.Tree)]
        [DataSourceProperty]
        public bool CanAddUpgradeTarget => UpgradeController.CanAddUpgradeTarget();

        [DataSourceProperty]
        public string AddUpgradeTargetText => L.S("add_upgrade_target_text", "Add Upgrade");

        [DataSourceMethod]
        public void ExecuteAddUpgradeTarget() => UpgradeController.AddUpgradeTarget();
    }
}
