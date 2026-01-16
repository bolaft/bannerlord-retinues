using System;
using System.IO;
using System.Xml.Linq;
using Retinues.Utilities;

namespace Retinues.Framework.Modules.Mods
{
    /// <summary>
    /// Writes a SubmodProject to disk (module root + SubModule.xml + ModuleData files).
    /// Controllers should call this, then display UI messages based on ModWriteResult.
    /// </summary>
    public sealed class ModWriter
    {
        public ModWriteResult WriteToGameModules(ModProject project, bool overwrite)
        {
            if (project == null || project.Manifest == null)
                return ModWriteResult.Fail("Project was null.");

            if (
                !ModEnvironment.TryGetGameModulesDirectory(out var modulesDir)
                || string.IsNullOrWhiteSpace(modulesDir)
            )
                return ModWriteResult.Fail("Could not locate the game's Modules folder.");

            var moduleRoot = Path.Combine(modulesDir, project.Manifest.Id);
            return WriteToDirectory(moduleRoot, project, overwrite);
        }

        public ModWriteResult WriteToDirectory(
            string moduleRoot,
            ModProject project,
            bool overwrite
        )
        {
            try
            {
                if (project == null || project.Manifest == null)
                    return ModWriteResult.Fail("Project was null.");

                if (string.IsNullOrWhiteSpace(moduleRoot))
                    return ModWriteResult.Fail("Module root was empty.");

                var willOverwrite = Directory.Exists(moduleRoot);
                if (willOverwrite && !overwrite)
                    return ModWriteResult.Fail($"Target module already exists: {moduleRoot}");

                Directory.CreateDirectory(moduleRoot);

                // Always write SubModule.xml first.
                var subModulePath = Path.Combine(moduleRoot, "SubModule.xml");
                WriteXml(subModulePath, project.Manifest.ToXDocument());

                int filesWritten = 1;

                foreach (var f in project.Files)
                {
                    if (f == null || string.IsNullOrWhiteSpace(f.RelativePath))
                        continue;

                    var abs = f.GetAbsolutePath(moduleRoot);
                    Directory.CreateDirectory(Path.GetDirectoryName(abs) ?? moduleRoot);

                    switch (f.Kind)
                    {
                        case ModFileKind.Text:
                            File.WriteAllText(abs, f.TextContent ?? string.Empty, Utf8.NoBom);
                            filesWritten++;
                            break;

                        case ModFileKind.Xml:
                            WriteXml(abs, f.XmlContent);
                            filesWritten++;
                            break;

                        case ModFileKind.Bytes:
                            File.WriteAllBytes(abs, f.ByteContent ?? []);
                            filesWritten++;
                            break;
                    }
                }

                Log.Debug($"Wrote submod '{project.Manifest.Id}' ({filesWritten} files).");

                return ModWriteResult.Ok(
                    moduleId: project.Manifest.Id,
                    moduleRoot: moduleRoot,
                    subModuleXmlPath: subModulePath,
                    overwroteExisting: willOverwrite,
                    filesWritten: filesWritten
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "SubmodWriter.WriteToDirectory failed.");
                return ModWriteResult.Fail(ex.Message, ex);
            }
        }

        private static void WriteXml(string path, XDocument doc)
        {
            if (string.IsNullOrWhiteSpace(path) || doc == null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            XML.SaveDocumentUtf8NoBom(path, doc, indent: true, omitXmlDeclaration: false);
        }
    }

    public sealed class ModWriteResult
    {
        public bool Success { get; }
        public string Error { get; }
        public Exception Exception { get; }

        public string ModuleId { get; }
        public string ModuleRoot { get; }
        public string SubModuleXmlPath { get; }

        public bool OverwroteExisting { get; }
        public int FilesWritten { get; }

        private ModWriteResult(
            bool success,
            string error,
            Exception exception,
            string moduleId,
            string moduleRoot,
            string subModuleXmlPath,
            bool overwroteExisting,
            int filesWritten
        )
        {
            Success = success;
            Error = error ?? string.Empty;
            Exception = exception;

            ModuleId = moduleId ?? string.Empty;
            ModuleRoot = moduleRoot ?? string.Empty;
            SubModuleXmlPath = subModuleXmlPath ?? string.Empty;

            OverwroteExisting = overwroteExisting;
            FilesWritten = filesWritten;
        }

        public static ModWriteResult Ok(
            string moduleId,
            string moduleRoot,
            string subModuleXmlPath,
            bool overwroteExisting,
            int filesWritten
        ) =>
            new(
                success: true,
                error: null,
                exception: null,
                moduleId: moduleId,
                moduleRoot: moduleRoot,
                subModuleXmlPath: subModuleXmlPath,
                overwroteExisting: overwroteExisting,
                filesWritten: filesWritten
            );

        public static ModWriteResult Fail(string error, Exception ex = null) =>
            new(
                success: false,
                error: error,
                exception: ex,
                moduleId: null,
                moduleRoot: null,
                subModuleXmlPath: null,
                overwroteExisting: false,
                filesWritten: 0
            );
    }
}
