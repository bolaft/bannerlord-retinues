using System.Threading;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Views;
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

        internal void ScrollToSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return;

            _sectionScrollTarget = sectionName;
            _sectionScrollVersion++;

            // Section VM bindings are evaluated on the section objects, so refresh them.
            if (Sections != null)
            {
                for (int i = 0; i < Sections.Count; i++)
                    Sections[i]?.RefreshScrollBindings();
            }
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
        public override int Width => 1200;

        [DataSourceProperty]
        public override HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Center;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sections                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<SectionVM> Sections { get; private set; }
    }
}
