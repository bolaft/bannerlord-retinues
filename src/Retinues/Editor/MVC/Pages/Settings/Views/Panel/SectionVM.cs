using Retinues.Editor.Events;
using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// ViewModel for a single settings section.
    /// </summary>
    public sealed class SectionVM : EventListenerVM
    {
        private readonly SettingsPanelVM _panel;
        private readonly Section _section;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SectionVM(SettingsPanelVM panel, Section section)
        {
            _panel = panel;
            _section = section;
            Name = section.Name;
            Description = section.Description;
            Options = [];

            foreach (
                var option in Retinues.Settings.ConfigurationManager.GetOptionsInSection(
                    section.Name
                )
            )
                Options.Add(OptionVM.Create(option));

            OnPropertyChanged(nameof(Options));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Section Scrolling                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsScrollTarget =>
            _panel != null
            && !string.IsNullOrWhiteSpace(_panel.SectionScrollTarget)
            && string.Equals(_panel.SectionScrollTarget, Name, System.StringComparison.Ordinal);

        [DataSourceProperty]
        public bool IsActiveSection =>
            _panel != null
            && !string.IsNullOrWhiteSpace(_panel.ActiveSection)
            && string.Equals(_panel.ActiveSection, Name, System.StringComparison.Ordinal);

        [DataSourceProperty]
        public int AutoScrollVersion => _panel?.SectionScrollVersion ?? 0;

        [DataSourceProperty]
        public string AutoScrollScope => _panel?.SectionScrollScope ?? "SettingsSections";

        public void ExecuteScrollToSection()
        {
            _panel?.ScrollToSection(Name);
        }

        [DataSourceProperty]
        public bool ShowInQuickNav =>
            _section != null
            && !ReferenceEquals(_section, Configuration.SkillCaps)
            && !ReferenceEquals(_section, Configuration.SkillTotals);

        internal void RefreshScrollBindings()
        {
            OnPropertyChanged(nameof(IsScrollTarget));
            OnPropertyChanged(nameof(AutoScrollVersion));
            OnPropertyChanged(nameof(AutoScrollScope));
        }

        internal void RefreshActiveSection()
        {
            OnPropertyChanged(nameof(IsActiveSection));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name { get; private set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Description                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Description { get; private set; }

        [DataSourceProperty]
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Options                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<OptionVM> Options { get; private set; }
    }
}
