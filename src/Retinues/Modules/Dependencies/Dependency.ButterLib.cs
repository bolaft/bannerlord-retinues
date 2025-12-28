using Retinues.Utilities;

namespace Retinues.Modules.Dependencies
{
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

            MarkInitialized();
            Log.Info("[ButterLib] ButterLib present.");
        }

        public override void Shutdown()
        {
            IsInitialized = false;
        }
    }
}
