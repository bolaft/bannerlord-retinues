using System;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Retinues.Utilities;

namespace Retinues.Module.Dependencies
{
    public enum DependencyKind
    {
        Required,
        Recommended,
        Optional,
    }

    public enum DependencyState
    {
        MissingModule,
        PresentButNotInitialized,
        Initialized,
    }

    /// <summary>
    /// Base class for a single external dependency (Harmony, UIExtenderEx, MCM, ButterLib, etc.).
    /// Provides common state & helpers; derived classes implement Initialize/Shutdown.
    /// </summary>
    public abstract class Dependency(string moduleId, string displayName, DependencyKind kind)
    {
        public string ModuleId { get; } = moduleId;

        /// <summary>
        /// Additional module IDs that count as satisfying this dependency (e.g. legacy IDs).
        /// </summary>
        public string DisplayName { get; } = displayName;

        public DependencyKind Kind { get; } = kind;

        public bool IsModuleLoaded => ModuleManager.IsLoaded(ModuleId);

        public bool IsInitialized { get; protected set; }

        public DependencyState State
        {
            get
            {
                if (!IsModuleLoaded)
                    return DependencyState.MissingModule;

                if (!IsInitialized)
                    return DependencyState.PresentButNotInitialized;

                return DependencyState.Initialized;
            }
        }

        /// <summary>
        /// Helper for derived classes once they successfully complete their initialization logic.
        /// </summary>
        protected void MarkInitialized()
        {
            if (IsModuleLoaded)
                IsInitialized = true;
        }

        /// <summary>
        /// Helper for derived classes to mark that initialization failed even though the module is loaded.
        /// </summary>
        protected void MarkError()
        {
            if (IsModuleLoaded)
                IsInitialized = false;
        }

        /// <summary>
        /// Performs dependency-specific initialization (patches, UIExtenderEx registration, etc.).
        /// Should be idempotent.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Performs dependency-specific teardown (unpatch, disable UIExtenderEx, etc.).
        /// Should be idempotent.
        /// </summary>
        public abstract void Shutdown();

        public override string ToString()
        {
            return string.Format(
                "{0} ({1}) - {2}, loaded={3}, initialized={4}",
                DisplayName,
                ModuleId,
                Kind,
                IsModuleLoaded,
                IsInitialized
            );
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                         Harmony                        //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Harmony dependency: applies patches + safe methods + compatibility patches.
    /// Mirrors the old SubModule behavior.
    /// </summary>
    public sealed class HarmonyDependency : Dependency
    {
        private const string HarmonyInstanceId = "Retinues";

        private Harmony _harmony;

        public HarmonyDependency()
            : base(
                moduleId: "Bannerlord.Harmony", // adjust if needed
                displayName: "Harmony",
                kind: DependencyKind.Required
            ) { }

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

        public override void Shutdown()
        {
            if (_harmony == null)
                return;

            try
            {
                _harmony.UnpatchAll(HarmonyInstanceId);
                Log.Debug("[Harmony] Harmony patches removed.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "[Harmony] Error while removing Harmony patches.");
            }
            finally
            {
                _harmony = null;
                IsInitialized = false;
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                      UIExtenderEx                      //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// UIExtenderEx dependency: registers & enables Retinues UI mixins.
    /// Mirrors the old SubModule EnableUIExtender logic.
    /// </summary>
    public sealed class UIExtenderExDependency : Dependency
    {
        private UIExtender _extender;

        public UIExtenderExDependency()
            : base(
                moduleId: "Bannerlord.UIExtenderEx",
                displayName: "UIExtenderEx",
                kind: DependencyKind.Required
            ) { }

        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Error("[UIExtenderEx] UIExtenderEx module not loaded; UI patches disabled.");
                MarkError();
                return;
            }

            if (_extender != null)
                return; // Already initialized

            try
            {
                var asm = typeof(UIExtenderExDependency).Assembly;

                _extender = UIExtender.Create("Retinues");
                _extender.Register(asm);
                _extender.Enable();

                MarkInitialized();
                Log.Debug("[UIExtenderEx] UIExtenderEx enabled & Retinues assembly registered.");
            }
            catch (Exception e)
            {
                MarkError();
                Log.Exception(e, "[UIExtenderEx] Enabling UIExtenderEx failed.");
            }
        }

        public override void Shutdown()
        {
            if (_extender == null)
                return;

            try
            {
                _extender.Disable();
                Log.Debug("[UIExtenderEx] UIExtenderEx disabled.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "[UIExtenderEx] Disabling UIExtenderEx failed.");
            }
            finally
            {
                _extender = null;
                IsInitialized = false;
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                         MCM v5                         //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// MCM dependency: handles delayed registration with MCM by retrying
    /// around the initial module screen setup.
    /// </summary>
    public sealed class MCMDependency : Dependency
    {
        private int _retryCount;
        private bool _registrationSucceeded;

        // ~5 seconds at 60 FPS
        private const int MaxRetries = 300;

        public MCMDependency()
            : base(
                moduleId: "Bannerlord.MBOptionScreen",
                displayName: "Mod Configuration Menu v5",
                kind: DependencyKind.Recommended
            ) { }

        /// <summary>
        /// Called from SubModule.OnSubModuleLoad.
        /// We only verify presence here and defer actual registration
        /// to the initial-screen / tick callbacks.
        /// </summary>
        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Info("[MCM] MCM module not loaded; configuration UI will be unavailable.");
                MarkError();
                return;
            }

            Log.Info(
                "[MCM] MCM module detected; waiting for UI to become ready before registering."
            );
            // Real work happens in OnBeforeInitialModuleScreenSetAsRoot / OnApplicationTick.
        }

        public override void Shutdown()
        {
            _registrationSucceeded = false;
            _retryCount = 0;
            IsInitialized = false;
        }

        public void TryRegister()
        {
            if (_registrationSucceeded)
                return;

            if (!IsModuleLoaded)
                return;

            if (_retryCount >= MaxRetries)
                return;

            _retryCount++;

            bool ok = Configuration.MCM.Register();
            if (!ok)
            {
                // Do not spam; only log when first starting to retry.
                if (_retryCount == 1)
                    Log.Info("[MCM] Registration not yet accepted; will retry for a few seconds.");
                return;
            }

            _registrationSucceeded = true;
            MarkInitialized();
            Log.Info("[MCM] Registration succeeded.");
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                        ButterLib                       //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// ButterLib dependency. For now, presence == initialized.
    /// </summary>
    public sealed class ButterLibDependency : Dependency
    {
        public ButterLibDependency()
            : base(
                moduleId: "Bannerlord.ButterLib",
                displayName: "ButterLib",
                kind: DependencyKind.Recommended
            ) { }

        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Warn("[ButterLib] ButterLib module not loaded; some features may be disabled.");
                MarkError();
                return;
            }

            // Nothing special to wire; just mark as initialized.
            MarkInitialized();
            Log.Debug("[ButterLib] ButterLib present.");
        }

        public override void Shutdown()
        {
            // No special teardown required at the moment.
            IsInitialized = false;
        }
    }
}
