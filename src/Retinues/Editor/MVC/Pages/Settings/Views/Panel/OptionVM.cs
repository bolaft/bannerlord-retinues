using System;
using System.Collections;
using Retinues.Editor.Events;
using Retinues.Interface.Components;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// ViewModel for an option.
    /// </summary>
    public abstract class OptionVM : EventListenerVM
    {
        protected readonly IOption Option;
        private readonly MBBindingList<DescriptionLineVM> _descriptionLines = [];

        /// <summary>
        /// Creates a ViewModel wrapper around a configuration option.
        /// </summary>
        protected OptionVM(IOption option)
        {
            Option = option;
            RebuildDescriptionLines();
            Retinues.Settings.ConfigurationManager.OptionChanged += OnOptionChanged;
        }

        /// <summary>
        /// Unsubscribes from events and releases resources.
        /// </summary>
        public override void OnFinalize()
        {
            Retinues.Settings.ConfigurationManager.OptionChanged -= OnOptionChanged;
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => Option.Name;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Description                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Description => Option.Description;

        [DataSourceProperty]
        public MBBindingList<DescriptionLineVM> DescriptionLines => _descriptionLines;

        [DataSourceProperty]
        public bool HasDescription => _descriptionLines.Count > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Status                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool IsDisabled => (Option?.IsDisabled ?? true) || !IsDependencySatisfied();

        [DataSourceProperty]
        public bool IsEnabled => !IsDisabled;

        [DataSourceProperty]
        public virtual bool IsVisible => IsDependencySatisfied();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Typed Value API                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // NOTE: Gauntlet still evaluates/binds attributes even when IsVisible=false.
        // These typed properties (with safe defaults) ensure the templates can bind
        // without reflection cast exceptions when the control is not applicable.

        [DataSourceProperty]
        public virtual bool BoolValue
        {
            get => false;
            set { }
        }

        [DataSourceProperty]
        public virtual int IntValue
        {
            get => 0;
            set { }
        }

        [DataSourceProperty]
        public virtual float FloatValue
        {
            get => 0f;
            set { }
        }

        [DataSourceProperty]
        public virtual int IntMin => 0;

        [DataSourceProperty]
        public virtual int IntMax => 1;

        [DataSourceProperty]
        public virtual float FloatMin => 0f;

        [DataSourceProperty]
        public virtual float FloatMax => 1f;

        [DataSourceProperty]
        public virtual string ValueText => string.Empty;

        [DataSourceProperty]
        public virtual string SelectedLabel => string.Empty;

        [DataSourceProperty]
        public virtual Tooltip Tooltip => null;

        /// <summary>
        /// Toggles the option value for boolean-like controls.
        /// </summary>
        public virtual void ExecuteToggle() { }

        /// <summary>
        /// Selects the previous choice for multi-choice controls.
        /// </summary>
        public virtual void ExecuteSelectPrevious() { }

        /// <summary>
        /// Selects the next choice for multi-choice controls.
        /// </summary>
        public virtual void ExecuteSelectNext() { }

        /// <summary>
        /// Opens the choice selection popup for multi-choice controls.
        /// </summary>
        public virtual void ExecuteOpenChoicePopup() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public virtual bool IsBoolOption => false;

        [DataSourceProperty]
        public virtual bool IsEnumOption => false;

        [DataSourceProperty]
        public virtual bool IsIntSliderOption => false;

        [DataSourceProperty]
        public virtual bool IsFloatSliderOption => false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Factory                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates the appropriate ViewModel type for the option.
        /// </summary>
        public static OptionVM Create(IOption option)
        {
            if (option == null)
                return new UnsupportedOptionVM(null);

            var t = option.Type;
            if (t == typeof(bool))
                return new BoolOptionVM(option);

            if (t != null && t.IsEnum)
                return new EnumOptionVM(option);

            if (t == typeof(int))
                return new IntSliderOptionVM(option);

            if (t == typeof(float))
                return new FloatSliderOptionVM(option);

            return new UnsupportedOptionVM(option);
        }

        /// <summary>
        /// Handles updates when this option's value changes externally.
        /// </summary>
        protected virtual void OnOptionValueChanged(object newValue) { }

        /// <summary>
        /// Builds line-wrapped description entries for Gauntlet rendering.
        /// </summary>
        private void RebuildDescriptionLines()
        {
            _descriptionLines.Clear();

            var description = Option?.Description ?? string.Empty;
            if (string.IsNullOrEmpty(description))
                return;

            // Gauntlet doesn't respect right-alignment across embedded newlines.
            // Split into separate widgets via ItemTemplate instead.
            var wrapped = Format.WrapWhitespace(description, 70);
            var normalized = (wrapped ?? string.Empty).Replace("\r\n", "\n");
            foreach (var line in normalized.Split('\n'))
            {
                var displayLine = line.Length == 0 ? " " : line;
                _descriptionLines.Add(new DescriptionLineVM(displayLine));
            }

            OnPropertyChanged(nameof(HasDescription));
            OnPropertyChanged(nameof(DescriptionLines));
        }

        /// <summary>
        /// Responds to global option changes by updating bindings and visibility.
        /// </summary>
        private void OnOptionChanged(string key, object newValue)
        {
            if (Option == null)
                return;

            try
            {
                var isSelf = string.Equals(Option.Key, key, StringComparison.OrdinalIgnoreCase);

                var isDependencyOrAncestor = IsAffectedByDependencyChange(key);

                // Ignore unrelated option changes.
                if (!isSelf && !isDependencyOrAncestor)
                    return;

                // Update our own value bindings.
                // Important: when our dependency changes, our *effective* value can change
                // (e.g. dependsOnDisabledOverride), so we must refresh the value bindings too.
                if (isSelf)
                {
                    OnOptionValueChanged(newValue);
                }
                else if (isDependencyOrAncestor)
                {
                    try
                    {
                        OnOptionValueChanged(Option.GetObject());
                    }
                    catch
                    {
                        // Edge-safe.
                    }
                }

                OnPropertyChanged(nameof(IsDisabled));
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(IsVisible));
            }
            catch
            {
                // Edge-safe.
            }
        }

        /// <summary>
        /// Returns true if <paramref name="key"/> matches this option's dependency key
        /// or any ancestor dependency key.
        /// </summary>
        private bool IsAffectedByDependencyChange(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || Option == null)
                return false;

            // Walk DependsOn chain: A depends on B depends on C...
            // If any of B/C/... changed, A's effective value / visibility can change.
            var current = Option.DependsOn;
            int safety = 0;
            while (current != null && safety++ < 32)
            {
                if (string.Equals(current.Key, key, StringComparison.OrdinalIgnoreCase))
                    return true;

                current = current.DependsOn;
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether this option's dependency condition is currently satisfied.
        /// </summary>
        private bool IsDependencySatisfied()
        {
            var dependsOn = Option?.DependsOn;
            if (dependsOn == null)
                return true;

            object current;
            try
            {
                current = dependsOn.GetObject();
            }
            catch
            {
                return true;
            }

            object expected = Option.DependsOnValue;

            // Common case: depends on a bool "enabled" toggle.
            if (expected == null && dependsOn.Type == typeof(bool))
                expected = true;

            // If no expected value is provided for non-bool dependencies, treat it as always satisfied.
            if (expected == null)
                return true;

            // Support sets of acceptable values (e.g. new[] { Mode.A, Mode.B }).
            if (expected is IEnumerable enumerable && expected is not string)
            {
                foreach (var allowed in enumerable)
                {
                    if (Equals(current, allowed))
                        return true;
                }
                return false;
            }

            return Equals(current, expected);
        }
    }
}
