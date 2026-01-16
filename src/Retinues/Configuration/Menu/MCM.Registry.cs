using System;
using Retinues.Utilities;

namespace Retinues.Configuration.Menu
{
    /// <summary>
    /// MCM interop wrapper for Retinues settings.
    /// Generic settings logic lives in SettingsManager and Settings.
    /// </summary>
    public static partial class MCM
    {
        /* ━━━━━━━━ Statics ━━━━━━━ */

        private static bool _isRegistered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Registration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Register the configuration page and options with MCM (returns true when successful).
        /// Can safely be called many times; after first success it is a cheap no-op.
        /// </summary>
        public static bool Register()
        {
            if (_isRegistered)
                return true;

            try
            {
                Log.Debug("Attempting to register Retinues with MCM...");

                // Ensure SettingsManager has discovered all options
                var _ = SettingsManager.AllOptions;

                var ok = Build();
                if (!ok)
                {
                    // Builder not ready or provider refused the page this tick
                    return false;
                }

                _isRegistered = true;
                Log.Debug("Retinues options registered with MCM.");

                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, "MCM.Register failed.");
                return false;
            }
        }
    }
}
