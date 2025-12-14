using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
            SkillsRow1 = [];
            SkillsRow2 = [];

            var skills = WCharacter.GetSkillList(State.Character);

            foreach (var skill in skills.Take(4))
            {
                var skillVM = new CharacterSkillVM(skill);
                SkillsRow1.Add(skillVM);
            }

            foreach (var skill in skills.Skip(4).Take(4))
            {
                var skillVM = new CharacterSkillVM(skill);
                SkillsRow2.Add(skillVM);
            }

            OnPropertyChanged(nameof(SkillsRow1));
            OnPropertyChanged(nameof(SkillsRow2));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Mode)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Mode == EditorMode.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string NameHeaderText => L.S("name_header_text", "Name");

        [EventListener(UIEvent.Character, UIEvent.Name)]
        [DataSourceProperty]
        public string NameText => State.Character.Name;

        /// <summary>
        /// Prompt to rename the selected character.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteRename()
        {
            Inquiries.TextInputPopup(
                title: L.T("rename_troop", "Rename Troop"),
                defaultInput: State.Character.Name,
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
        public string CultureText
        {
            get
            {
                var culture = State.Character?.Culture;
                var name = culture?.Name;

                if (!string.IsNullOrWhiteSpace(name))
                    return name;

                return L.S("unknown", "Unknown");
            }
        }

        /// <summary>
        /// Change the selected character's culture.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteChangeCulture()
        {
            var elements = new List<InquiryElement>();

            foreach (var culture in WCulture.All)
            {
                var imageIdentifier = culture.ImageIdentifier;
                var name = culture.Name;

                if (imageIdentifier == null || name == null)
                    continue; // Probably unusable modded culture, skip.

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
        public MBBindingList<CharacterSkillVM> SkillsRow1 { get; set; }

        [DataSourceProperty]
        public MBBindingList<CharacterSkillVM> SkillsRow2 { get; set; }

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
