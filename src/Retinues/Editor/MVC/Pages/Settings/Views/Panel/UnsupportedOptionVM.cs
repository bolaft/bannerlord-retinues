using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Fallback VM for options that don't have a specialized editor yet.
    /// Keeps the settings page resilient.
    /// </summary>
    public sealed class UnsupportedOptionVM(IOption option) : OptionVM(option)
    {
        [DataSourceProperty]
        public override string ValueText
        {
            get
            {
                if (Option == null)
                    return string.Empty;

                try
                {
                    return (Option.GetObject() ?? string.Empty).ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        [DataSourceProperty]
        public string TypeName => Option?.Type?.Name ?? string.Empty;

        protected override void OnOptionValueChanged(object newValue)
        {
            OnPropertyChanged(nameof(ValueText));
        }
    }
}
