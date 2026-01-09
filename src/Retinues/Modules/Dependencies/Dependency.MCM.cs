using Retinues.Utilities;

namespace Retinues.Modules.Dependencies
{
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

        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Debug("[MCM] MCM module not loaded; configuration UI will be unavailable.");
                MarkError();
                return;
            }

            Log.Debug(
                "[MCM] MCM module detected; waiting for UI to become ready before registering."
            );
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

            bool ok = Configuration.MCM.MCMBuilder.Register();
            if (!ok)
            {
                if (_retryCount == 1)
                    Log.Debug("[MCM] Registration not yet accepted; will retry for a few seconds.");
                return;
            }

            _registrationSucceeded = true;
            MarkInitialized();
            Log.Debug("[MCM] Registration succeeded.");
        }
    }
}
