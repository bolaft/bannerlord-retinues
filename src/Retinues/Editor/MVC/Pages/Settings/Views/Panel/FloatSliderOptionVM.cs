using System;
using System.Globalization;
using Retinues.Settings;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Slider option for float settings.
    /// </summary>
    public sealed class FloatSliderOptionVM(IOption option) : OptionVM(option)
    {
        public override bool IsFloatSliderOption => true;

        private const float Step = 0.05f;

        [DataSourceProperty]
        public override float FloatMin => (float)(Option?.MinValue ?? 0);

        [DataSourceProperty]
        public override float FloatMax
        {
            get
            {
                float min = FloatMin;
                float max = (float)(Option?.MaxValue ?? 0);
                if (max < min)
                    max = min;
                return max;
            }
        }

        [DataSourceProperty]
        public override float FloatValue
        {
            get
            {
                if (Option == null)
                    return 0f;

                try
                {
                    float raw = Convert.ToSingle(Option.GetObject(), CultureInfo.InvariantCulture);
                    float min = FloatMin;
                    float max = FloatMax;
                    return SnapToStepMultipleInRange(raw, min, max, Step, SnapMode.Nearest);
                }
                catch
                {
                    return 0f;
                }
            }
            set
            {
                if (Option == null || IsDisabled)
                    return;

                float min = FloatMin;
                float max = FloatMax;

                float current = FloatValue;
                if (Math.Abs(value - current) <= float.Epsilon)
                    return;

                // Direction-aware snapping so small adjustments move to the next/previous step.
                SnapMode mode = value > current ? SnapMode.Up : SnapMode.Down;
                float snapped = SnapToStepMultipleInRange(value, min, max, Step, mode);

                Option.SetObject(snapped);
                OnPropertyChanged(nameof(FloatValue));
                OnPropertyChanged(nameof(ValueText));
            }
        }

        [DataSourceProperty]
        public override string ValueText =>
            (FloatValue * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";

        private enum SnapMode
        {
            Nearest,
            Up,
            Down,
        }

        /// <summary>
        /// Snaps a value to a multiple of step within a min/max range.
        /// If min is not itself a multiple, min is treated as a special-case floor.
        /// Example: min=0.01, step=0.05 => 0.01, 0.05, 0.10, 0.15...
        /// </summary>
        private static float SnapToStepMultipleInRange(
            float value,
            float min,
            float max,
            float step,
            SnapMode mode
        )
        {
            if (step <= 0f)
            {
                if (value < min)
                    return min;
                if (value > max)
                    return max;
                return value;
            }

            if (max < min)
                max = min;

            if (value <= min)
                return min;

            if (value >= max)
                return max;

            try
            {
                // Snap to a multiple of step (relative to 0), with min acting as a floor special-case.
                // Use decimal math to avoid float precision artifacts around boundaries.
                decimal d = (decimal)value;
                decimal dMin = (decimal)min;
                decimal dMax = (decimal)max;
                decimal s = (decimal)step;

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

                return (float)snapped;
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

        /// <summary>
        /// Refreshes slider bindings when the underlying option value changes.
        /// </summary>
        protected override void OnOptionValueChanged(object newValue)
        {
            OnPropertyChanged(nameof(FloatValue));
            OnPropertyChanged(nameof(ValueText));
        }
    }
}
