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
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SettingsPanelVM()
        {
            Sections = [];

            foreach (var section in Retinues.Settings.ConfigurationManager.Sections)
                Sections.Add(new SectionVM(section));

            OnPropertyChanged(nameof(Sections));
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
        //                        Sections                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<SectionVM> Sections { get; private set; }
    }
}
