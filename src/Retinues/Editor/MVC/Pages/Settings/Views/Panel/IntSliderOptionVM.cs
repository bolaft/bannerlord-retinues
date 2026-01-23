using System;
using System.Globalization;
using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Slider option for integer settings.
    /// </summary>
    public sealed class IntSliderOptionVM(IOption option) : OptionVM(option)
    {
        public override bool IsIntSliderOption => true;

        private const int SmallStep = 1;
        private const int LargeStep = 5;
        private const int LargeStepThresholdMax = 200;

        [DataSourceProperty]
        public override int IntMin => (int)Math.Round(Option?.MinValue ?? 0);

        [DataSourceProperty]
        public override int IntMax
        {
            get
            {
                int min = IntMin;
                int max = (int)Math.Round(Option?.MaxValue ?? 0);
                if (max < min)
                    max = min;
                return max;
            }
        }

        [DataSourceProperty]
        public override int IntValue
        {
            get
            {
                if (Option == null)
                    return 0;

                try
                {
                    int raw = Convert.ToInt32(Option.GetObject(), CultureInfo.InvariantCulture);
                    int min = IntMin;
                    int max = IntMax;
                    int step = GetStepForRange(max);
                    return SnapToStepMultipleInRange(raw, min, max, step, SnapMode.Nearest);
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                if (Option == null || IsDisabled)
                    return;

                int min = IntMin;
                int max = IntMax;
                int step = GetStepForRange(max);
                int current = IntValue;
                if (value == current)
                    return;

                // Direction-aware snapping so +/-1 behaves like +/-step when stepping is enabled.
                SnapMode mode = value > current ? SnapMode.Up : SnapMode.Down;
                int snapped = SnapToStepMultipleInRange(value, min, max, step, mode);

                Option.SetObject(snapped);
                OnPropertyChanged(nameof(IntValue));
                OnPropertyChanged(nameof(ValueText));
            }
        }

        [DataSourceProperty]
        public override string ValueText => IntValue.ToString(CultureInfo.InvariantCulture);

        private static int GetStepForRange(int max) =>
            max >= LargeStepThresholdMax ? LargeStep : SmallStep;

        private enum SnapMode
        {
            Nearest,
            Up,
            Down,
        }

        /// <summary>
        /// Snaps a value to a multiple of step within a min/max range.
        /// If min is not itself a multiple, min is treated as a special-case floor.
        /// Example: min=1, step=5 => 1, 5, 10, 15...
        /// </summary>
        private static int SnapToStepMultipleInRange(
            int value,
            int min,
            int max,
            int step,
            SnapMode mode
        )
        {
            if (max < min)
                max = min;

            if (value <= min)
                return min;

            if (value >= max)
                return max;

            if (step <= 1)
                return value;

            try
            {
                // Snap to a multiple of step (relative to 0), with min acting as a floor special-case.
                decimal d = value;
                decimal dMin = min;
                decimal dMax = max;
                decimal s = step;

                decimal scaled = d / s;
                decimal snappedScaled = mode switch
                {
                    SnapMode.Up => (decimal)Math.Ceiling((double)scaled),
                    SnapMode.Down => (decimal)Math.Floor((double)scaled),
                    _ => decimal.Round(scaled, 0, MidpointRounding.AwayFromZero),
                };

                decimal snapped = snappedScaled * s;

                if (snapped < dMin)
                    snapped = dMin;
                if (snapped > dMax)
                    snapped = dMax;

                return (int)snapped;
            }
            catch
            {
                if (value < min)
                    return min;
                if (value > max)
                    return max;
                return value;
            }
        }

        protected override void OnOptionValueChanged(object newValue)
        {
            OnPropertyChanged(nameof(IntValue));
            OnPropertyChanged(nameof(ValueText));
        }
    }
}
