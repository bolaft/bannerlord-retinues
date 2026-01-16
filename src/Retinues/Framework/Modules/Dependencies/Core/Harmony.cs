using System;
using HarmonyLib;
using Retinues.Utilities;

namespace Retinues.Framework.Modules.Dependencies.Core
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                         Harmony                        //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Harmony dependency: applies patches + safe methods + compatibility patches.
    /// Mirrors the old SubModule behavior.
    /// </summary>
    public sealed class HarmonyDependency : BaseDependency
    {
        private const string HarmonyInstanceId = "Retinues";

        private Harmony _harmony;

        public Harmony Harmony => _harmony;

        public HarmonyDependency()
            : base(
                moduleId: "Bannerlord.Harmony", // adjust if needed
                displayName: "Harmony",
                kind: DependencyKind.Required
            ) { }

        /// <summary>
        /// Initializes Harmony and applies patches.
        /// </summary>
        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Error("[Harmony] Harmony module not loaded; skipping patches.");
                MarkError();
                return;
            }

            if (_harmony != null)
                return; // Already initialized

            try
            {
                var asm = typeof(HarmonyDependency).Assembly;

                _harmony = new Harmony(HarmonyInstanceId);
                _harmony.PatchAll(asm);

                MarkInitialized();
                Log.Debug("[Harmony] Harmony patches applied.");
            }
            catch (Exception e)
            {
                MarkError();
                Log.Exception(e, "[Harmony] Error while applying Harmony patches.");
            }
        }

        /// <summary>
        /// Removes Harmony patches.
        /// </summary>
        public override void Shutdown()
        {
            if (_harmony == null)
                return;

            try
            {
                _harmony.UnpatchAll(HarmonyInstanceId);
                Log.Debug("[Harmony] Harmony patches removed.");
            }
#if DEBUG
            catch
            {
                // Ignore exceptions which may occur due to timer patches on safe classes
            }
#else
            catch (Exception e)
            {
                Log.Exception(e, "[Harmony] Error while removing Harmony patches.");
            }
#endif
            finally
            {
                _harmony = null;
                IsInitialized = false;
            }
        }
    }
}
