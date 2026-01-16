using System;
using MCM.Abstractions;
using Retinues.Utilities;

namespace Retinues.Configuration.Menu
{
    /// <summary>
    /// MCM interop wrapper for Retinues settings.
    /// Generic settings logic lives in SettingsManager and Settings.
    /// </summary>
    public static partial class MCM
    {
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
