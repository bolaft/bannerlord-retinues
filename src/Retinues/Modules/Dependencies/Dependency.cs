using System;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;

namespace Retinues.Modules.Dependencies
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
        VersionMismatch,
        PresentButNotInitialized,
        Initialized,
    }

    /// <summary>
    /// Base class for a single external dependency (Harmony, UIExtenderEx, MCM, ButterLib, etc.).
    /// Provides common state & helpers; derived classes implement Initialize/Shutdown.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
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

        /// <summary>
        /// Expected version read from this mod's SubModule.xml (DependedModules/DependedModule).
        /// Treated as a minimum required version when comparing.
        /// </summary>
        public string ExpectedVersion => ModuleManager.GetExpectedDependencyVersion(ModuleId);

        /// <summary>
        /// Actual loaded module version as reported by TaleWorlds.ModuleManager (via ModuleHelper).
        /// </summary>
        public string ActualVersion => ModuleManager.GetModule(ModuleId).Version;

        public bool HasExpectedVersion =>
            !string.IsNullOrWhiteSpace(ExpectedVersion)
            && !ExpectedVersion.Equals(
                ModuleManager.UnknownVersionString,
                StringComparison.OrdinalIgnoreCase
            );

        public bool IsVersionSatisfied
        {
            get
            {
                if (!IsModuleLoaded)
                    return false;

                if (!HasExpectedVersion)
                    return true;

                return ModuleManager.IsVersionAtLeast(ActualVersion, ExpectedVersion);
            }
        }

        public DependencyState State
        {
            get
            {
                if (!IsModuleLoaded)
                    return DependencyState.MissingModule;

                if (HasExpectedVersion && !IsVersionSatisfied)
                    return DependencyState.VersionMismatch;

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

        public string GetVersionDiagnostic()
        {
            if (!HasExpectedVersion)
                return ActualVersion;

            return L.T("version_expected", "{CURRENT} (expected >= {EXPECTED})")
                .SetTextVariable("CURRENT", ActualVersion)
                .SetTextVariable("EXPECTED", ExpectedVersion)
                .ToString();
        }

        public override string ToString()
        {
            return string.Format(
                "{0} ({1}) - {2}, loaded={3}, initialized={4}, version={5}",
                DisplayName,
                ModuleId,
                Kind,
                IsModuleLoaded,
                IsInitialized,
                GetVersionDiagnostic()
            );
        }
    }
}
