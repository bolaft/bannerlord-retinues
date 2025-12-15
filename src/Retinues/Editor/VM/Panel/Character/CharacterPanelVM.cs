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
        public void ExecuteRename()
        {
            Inquiries.TextInputPopup(
                title: L.T("rename_troop", "Rename Troop"),
                defaultInput: State.Character.Editable.Name,
                onConfirm: input => CharacterController.ChangeName(input.Trim()),
                description: L.T("enter_new_name", "Enter a new name:")
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string CultureHeaderText => L.S("culture_header_text", "Culture");

        [EventListener(UIEvent.Character, UIEvent.Culture)]
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string SkillsHeaderText => L.S("skills_header_text", "Skills");

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow1 { get; } = new();

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow2 { get; } = new();

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow3 { get; } = new();

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow4 { get; } = new();

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow5 { get; } = new(); // optional safety

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow6 { get; } = new(); // optional safety

        [DataSourceProperty]
        public int SkillsCount { get; private set; }

        [DataSourceProperty]
        public int SkillsGridRows { get; private set; }

        [DataSourceProperty]
        public int SkillsGridColumns { get; private set; }

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
                var vm = new CharacterSkillVM(list[i], layout);

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

            // Notify (not strictly required if list events propagate, but cheap + safe)
            OnPropertyChanged(nameof(SkillsCount));
            OnPropertyChanged(nameof(SkillsGridRows));
            OnPropertyChanged(nameof(SkillsGridColumns));
            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));
            OnPropertyChanged(nameof(SkillsRow3));
            OnPropertyChanged(nameof(SkillsRow4));
            OnPropertyChanged(nameof(SkillsRow5));
            OnPropertyChanged(nameof(SkillsRow6));
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

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool HasUpgradeSources => State.Character.UpgradeSources.Any();

        [DataSourceProperty]
        public string UpgradeSourcesHeaderText => L.S("upgrade_sources_header_text", "Origin");

        [EventListener(UIEvent.Character)]
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

        [EventListener(UIEvent.Character)]
        [DataSourceProperty]
        public bool HasUpgradeTargets => State.Character.UpgradeTargets.Any();

        [DataSourceProperty]
        public string UpgradeTargetsHeaderText => L.S("upgrade_targets_header_text", "Upgrades");

        [EventListener(UIEvent.Character)]
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
    }
}
