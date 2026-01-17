using Retinues.Utilities;

namespace Retinues.Framework.Modules.Dependencies.Core
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                        ButterLib                       //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// ButterLib dependency. For now, presence == initialized.
    /// </summary>
    public sealed class ButterLibDependency : BaseDependency
    {
        public ButterLibDependency()
            : base(
                moduleId: "Bannerlord.ButterLib",
                displayName: "ButterLib",
                kind: DependencyKind.Recommended
            ) { }

        /// <summary>
        /// Checks for ButterLib presence.
        /// </summary>
        public override void Initialize()
        {
            if (!IsModuleLoaded)
            {
                Log.Warning(
                    "[ButterLib] ButterLib module not loaded; some features may be disabled."
                );
                MarkError();
                return;
            }

            MarkInitialized();
            Log.Debug("[ButterLib] ButterLib present.");
        }

        /// <summary>
        /// Shutdown the dependency.
        /// </summary>
        public override void Shutdown()
        {
            IsInitialized = false;
        }
    }
}
