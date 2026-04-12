using System.Threading;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Settings.Controllers;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using Retinues.Interface.Widgets;
using Retinues.Settings;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Settings panel showing options for the selected subsection.
    /// </summary>
    public sealed class SettingsPanelVM : BasePanelVM
    {
        private static int _nextScrollScopeId;

        private int _sectionScrollVersion;
        private string _sectionScrollTarget;
        private readonly string _sectionScrollScope;

        private string _activeSection;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SettingsPanelVM()
        {
            _sectionScrollScope =
                $"SettingsSections:{Interlocked.Increment(ref _nextScrollScopeId)}";

            Sections = [];

            foreach (var section in Retinues.Settings.ConfigurationManager.Sections)
                Sections.Add(new SectionVM(this, section));

            OnPropertyChanged(nameof(Sections));

            ScrollSpyAnchorWidget.RegisterCallback(_sectionScrollScope, SetActiveSectionFromSpy);
        }

        /// <summary>
        /// Name key of the section we want to auto-scroll to.
        /// This intentionally uses the same value displayed in the UI.
        /// </summary>
        internal string SectionScrollTarget => _sectionScrollTarget;

        /// <summary>
        /// Monotonically increasing token used by AutoScrollButtonWidget.
        /// </summary>
        internal int SectionScrollVersion => _sectionScrollVersion;

        internal string SectionScrollScope => _sectionScrollScope;

        /// <summary>
        /// Name of the section currently visible at the top of the scroll area.
        /// Updated both by click-to-scroll and by the scroll-spy widget.
        /// </summary>
        internal string ActiveSection => _activeSection;

        internal void ScrollToSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return;

            _sectionScrollTarget = sectionName;
            _sectionScrollVersion++;

            // Highlight the nav button immediately; the spy will keep it updated during scroll.
            SetActiveSectionFromSpy(sectionName);

            // Section VM bindings are evaluated on the section objects, so refresh them.
            if (Sections != null)
            {
                for (int i = 0; i < Sections.Count; i++)
                    Sections[i]?.RefreshScrollBindings();
            }
        }

        /// <summary>
        /// Sections that are hidden from the quick-nav list get mapped to their
        /// visible parent so the nav highlight still follows them.
        /// </summary>
        private static string NormalizeNavSection(string sectionName)
        {
            if (
                string.Equals(
                    sectionName,
                    Configuration.SkillCaps.Name,
                    System.StringComparison.Ordinal
                )
                || string.Equals(
                    sectionName,
                    Configuration.SkillTotals.Name,
                    System.StringComparison.Ordinal
                )
            )
                return Configuration.Skills.Name;

            return sectionName;
        }

        /// <summary>
        /// Called by <see cref="ScrollSpyAnchorWidget"/> every frame with the name of
        /// the section currently visible at the top of the scroll area.
        /// </summary>
        private void SetActiveSectionFromSpy(string sectionName)
        {
            sectionName = NormalizeNavSection(sectionName);

            if (string.Equals(_activeSection, sectionName, System.StringComparison.Ordinal))
                return;

            _activeSection = sectionName;

            if (Sections != null)
            {
                for (int i = 0; i < Sections.Count; i++)
                    Sections[i]?.RefreshActiveSection();
            }
        }

        public override void OnFinalize()
        {
            ScrollSpyAnchorWidget.UnregisterCallback(_sectionScrollScope);
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        [EventListener(UIEvent.Page)]
        public bool OnSettingsPage => State.Page == EditorPage.Settings;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Layout                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override int Width => 1600;

        [DataSourceProperty]
        public override HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Center;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Purge                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string PurgeAllHeader => L.S("purge_all_header", "Uninstall");

        [DataSourceProperty]
        public Button<object> PurgeAllButton { get; } =
            new(
                action: PurgeController.PurgeAll,
                arg: () => null,
                refresh: [UIEvent.Page],
                labelFactory: () => L.S("purge_all_button", "Purge"),
                visibilityGate: () => State.Page == EditorPage.Settings
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Reset To Defaults                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string ResetToDefaultsHeader => L.S("presets_header", "Presets");

        [DataSourceProperty]
        public Button<object> ResetToDefaultsButton { get; } =
            new(
                action: SettingsController.ApplyDefaultPreset,
                arg: () => null,
                refresh: [UIEvent.Page],
                labelFactory: () => L.S("preset_default_button", "Default"),
                visibilityGate: () => State.Page == EditorPage.Settings
            );

        [DataSourceProperty]
        public Button<object> FreeformPresetButton { get; } =
            new(
                action: SettingsController.ApplyFreeformPreset,
                arg: () => null,
                refresh: [UIEvent.Page],
                labelFactory: () => L.S("preset_freeform_button", "Freeform"),
                visibilityGate: () => State.Page == EditorPage.Settings
            );

        [DataSourceProperty]
        public Button<object> RealisticPresetButton { get; } =
            new(
                action: SettingsController.ApplyRealisticPreset,
                arg: () => null,
                refresh: [UIEvent.Page],
                labelFactory: () => L.S("preset_realistic_button", "Realistic"),
                visibilityGate: () => State.Page == EditorPage.Settings
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sections                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<SectionVM> Sections { get; private set; }
    }
}
