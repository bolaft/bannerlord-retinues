using System;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using Retinues.Utilities;

namespace Retinues.Configuration.MCM
{
    /// <summary>
    /// MCM interop wrapper for Retinues settings.
    /// Generic settings logic lives in SettingsManager and Settings.
    /// </summary>
    public static partial class ConfigMenu
    {
        private const string MCMId = "Retinues.Settings";
        private const string MCMDisplay = "Retinues";
        private const string MCMFolder = "Retinues";
        private const string MCMFormat = "xml";

        private static FluentGlobalSettings _MCMSettings;
        private static object _MCMSettingsInstance;
        private static Type _MCMSettingsType;
        private static bool _isSyncingWithMCM;
        private static bool _isRegistered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
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

        /// <summary>
        /// Ask MCM to persist the current Retinues settings to disk.
        /// Uses MCM's DefaultSettingsProvider pipeline, so it behaves exactly
        /// like pressing 'Save' in the MCM UI.
        /// </summary>
        public static void Save()
        {
            try
            {
                if (_MCMSettings == null)
                {
                    Log.Info("MCM.Save called but _mcmSettings is null; skipping.");
                    return;
                }

                var provider = BaseSettingsProvider.Instance;
                if (provider == null)
                {
                    Log.Info(
                        "MCM.Save called but BaseSettingsProvider.Instance is null; skipping."
                    );
                    return;
                }

                provider.SaveSettings(_MCMSettings);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MCM.Save failed.");
            }
        }
    }
}
