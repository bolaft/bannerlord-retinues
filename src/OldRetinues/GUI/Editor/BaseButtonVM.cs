using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    /// <summary>
    /// Base view-model for clickable button-like elements.
    /// </summary>
    /// <remarks>
    /// Allow subclasses to opt out of global event registration.
    /// </remarks>
    public abstract class BaseButtonVM(bool autoRegister = true) : BaseVM(autoRegister)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Whether the button is currently enabled.
        /// </summary>
        [DataSourceProperty]
        public abstract bool IsEnabled { get; }

        /// <summary>
        /// Whether the button is currently selected.
        /// </summary>
        [DataSourceProperty]
        public abstract bool IsSelected { get; }
    }
}
