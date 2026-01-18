using System;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;

namespace Retinues.Framework.Modules.Dependencies
{
    /// <summary>
    /// Kind of dependency.
    /// </summary>
    public enum DependencyKind
    {
        Required,
        Recommended,
        Optional,
    }

    /// <summary>
    /// State of the dependency.
    /// </summary>
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
    public abstract class BaseDependency(string moduleId, string displayName, DependencyKind kind)
    {
        public string ModuleId { get; } = moduleId;

        /// <summary>
        /// Additional module IDs that count as satisfying this dependency (e.g. legacy IDs).
        /// </summary>
        public string DisplayName { get; } = displayName;

        public DependencyKind Kind { get; } = kind;

        public bool IsModuleLoaded => ModuleManager.IsLoaded(ModuleId);

        public bool IsInitialized { get; protected set; }

        // Expected minimum version read from this mod's SubModule.xml (DependedModules/DependedModule).
        public string ExpectedVersion => ModuleManager.GetExpectedDependencyVersion(ModuleId);

        // Actual loaded module version as reported by TaleWorlds.ModuleManager (via ModuleHelper).
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

        /// <summary>
        /// Gets a diagnostic string representing the version status of the dependency.
        /// </summary>
        public string GetVersionDiagnostic()
        {
            if (!HasExpectedVersion)
                return ActualVersion;

            return L.T("version_expected", "{CURRENT} (expected >= {EXPECTED})")
                .SetTextVariable("CURRENT", ActualVersion)
                .SetTextVariable("EXPECTED", ExpectedVersion)
                .ToString();
        }
    }
}
