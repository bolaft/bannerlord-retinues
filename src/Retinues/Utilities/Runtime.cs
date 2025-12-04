using System;
using System.IO;
using System.Reflection;

namespace Retinues.Utilities
{
    /// <summary>
    /// Helpers for module-wide runtime information (paths, assembly, environment).
    /// </summary>
    public static class Runtime
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Paths                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Lazy<string> _assemblyDirectory = new Lazy<string>(
            ResolveAssemblyDirectory
        );

        private static readonly Lazy<string> _moduleRoot = new Lazy<string>(ResolveModuleRoot);

        private static readonly Lazy<string> _moduleName = new Lazy<string>(ResolveModuleName);

        /// <summary>
        /// Directory containing the compiled Retinues DLL.
        /// Typically ".../Modules/Retinues/bin/Win64_Shipping_Client".
        /// </summary>
        public static string AssemblyDirectory => _assemblyDirectory.Value;

        /// <summary>
        /// Root directory of the Retinues module.
        /// Typically ".../Modules/Retinues".
        /// </summary>
        public static string ModuleRoot => _moduleRoot.Value;

        /// <summary>
        /// Name of the module folder (for example "Retinues").
        /// </summary>
        public static string ModuleName => _moduleName.Value;

        /// <summary>
        /// Combines the module root with one or more path segments.
        /// </summary>
        public static string GetPathInModule(params string[] relativeSegments)
        {
            if (relativeSegments == null || relativeSegments.Length == 0)
                return ModuleRoot;

            var path = ModuleRoot;
            for (int i = 0; i < relativeSegments.Length; i++)
            {
                var segment = relativeSegments[i];
                if (string.IsNullOrEmpty(segment))
                    continue;

                path = Path.Combine(path, segment);
            }

            return path;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Resolvers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string ResolveAssemblyDirectory()
        {
            try
            {
                var location = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var dir = Path.GetDirectoryName(location);
                    if (!string.IsNullOrEmpty(dir))
                        return dir;
                }
            }
            catch
            {
                // fall through
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string ResolveModuleRoot()
        {
            try
            {
                var dir = AssemblyDirectory;
                if (string.IsNullOrEmpty(dir))
                    return AppDomain.CurrentDomain.BaseDirectory;

                // Typical layout:
                //   .../Modules/Retinues/bin/Win64_Shipping_Client
                // We want:
                //   .../Modules/Retinues
                var parent = Directory.GetParent(dir);
                if (parent == null)
                    return dir;

                var maybeModuleRoot = parent.Parent;
                if (maybeModuleRoot != null)
                    return maybeModuleRoot.FullName;

                return parent.FullName;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private static string ResolveModuleName()
        {
            try
            {
                var root = ModuleRoot.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );
                var name = Path.GetFileName(root);
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
            catch
            {
                // fall through
            }

            try
            {
                return Assembly.GetExecutingAssembly().GetName().Name ?? "Retinues";
            }
            catch
            {
                return "Retinues";
            }
        }
    }
}
