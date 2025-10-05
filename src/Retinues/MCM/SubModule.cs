using Retinues.MCM.Options;
using TaleWorlds.MountAndBlade;

namespace Retinues.MCM
{
    /// <summary>
    /// Minimal SubModule used to ensure the MCM bootstrap is registered before the main menu is shown.
    /// It attempts registration once per frame until Bootstrap.Register returns true, then stops.
    /// </summary>
    public sealed class SubModule : MBSubModuleBase
    {
        /// <summary>
        /// Tracks whether bootstrap registration has succeeded to avoid repeated attempts.
        /// </summary>
        private bool _registered;

        /// <summary>
        /// Called during the module initialization phase before the initial root screen is set.
        /// Tries to register the Mod Configuration Menu once per frame; stops after success.
        /// </summary>
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            // Try once per frame until it succeeds (then stop).
            if (!_registered)
                _registered = Bootstrap.Register();
        }
    }
}
