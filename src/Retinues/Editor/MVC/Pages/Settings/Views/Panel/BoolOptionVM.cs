using System;
using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Checkbox option for bool settings.
    /// </summary>
    public sealed class BoolOptionVM(IOption option) : OptionVM(option)
    {
        public override bool IsBoolOption => true;

        [DataSourceProperty]
        public override bool BoolValue
        {
            get
            {
                if (Option == null)
                    return false;

                try
                {
                    return Convert.ToBoolean(Option.GetObject());
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                if (Option == null || IsDisabled)
                    return;

                Option.SetObject(value);
                OnPropertyChanged(nameof(BoolValue));
            }
        }

        /// <summary>
        /// Toggles the value of the option.
        /// </summary>
        public override void ExecuteToggle() => BoolValue = !BoolValue;

        /// <summary>
        /// Refreshes slider bindings when the underlying option value changes.
        /// </summary>
        protected override void OnOptionValueChanged(object newValue)
        {
            OnPropertyChanged(nameof(BoolValue));
        }
    }
}
