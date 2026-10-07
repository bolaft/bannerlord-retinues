using System;
using System.Linq;
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

                // Patch per class instead of one PatchAll: a single unresolvable target (for
                // example after a game update renames a method) then disables that one patch
                // class with a logged error, instead of aborting every remaining patch in the
                // assembly — which once turned a lone renamed hook into a total mod failure.
                int failed = 0;
                foreach (var type in AccessTools.GetTypesFromAssembly(asm))
                {
                    if (!IsPatchContainer(type))
                        continue;
                    try
                    {
                        _harmony.CreateClassProcessor(type).Patch();
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Log.Error($"[Harmony] Patching failed for {type.FullName}: {e.Message}");
                    }
                }

                if (failed > 0)
                    Log.Error($"[Harmony] {failed} patch class(es) failed to apply.");

                MarkInitialized();
                Log.Debug("[Harmony] Harmony patches applied.");
            }
            catch (Exception e)
            {
                MarkError();
                Log.Exception(e, "[Harmony] Error while applying Harmony patches.");
            }
        }

        // ClassProcessor also recognizes conventional names such as Cleanup and Prefix.
        // Only opt explicitly annotated containers into automatic discovery; interop
        // helpers that install their own patches must not be processed a second time.
        internal static bool IsPatchContainer(Type type) =>
            type.IsDefined(typeof(HarmonyPatch), false)
            || AccessTools.GetDeclaredMethods(type).Any(method =>
                method.IsDefined(typeof(HarmonyPatch), false));

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
