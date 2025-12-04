using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TaleWorlds.Library;

namespace Retinues.Utilities
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Success = 3,
        Warn = 4,
        Error = 5,
        Critical = 6,
    }

    /// <summary>
    /// Logging utility for diagnostics and debugging.
    /// Handles log levels, file and in-game output, exception reporting, and truncation.
    /// </summary>
    public static class Log
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Log Level Configuration                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string LogFileName = "debug.log";
        private const string InGamePrefix = "[Retinues] ";

        private static readonly object _fileLock = new();
        private static readonly string LogFile;

        private static bool DebugMode => true;

        /// <summary>
        /// Lowest level written to file.
        /// </summary>
        public static LogLevel MinFileLevel => DebugMode ? LogLevel.Trace : LogLevel.Info;

        /// <summary>
        /// Lowest level shown in-game (InformationManager).
        /// </summary>
        public static LogLevel MinInGameLevel => DebugMode ? LogLevel.Info : LogLevel.Critical;

        static Log()
        {
            try
            {
                LogFile = Runtime.GetPathInModule(LogFileName);
            }
            catch
            {
                LogFile = LogFileName;
            }
        }

        /// <summary>
        /// Initializes the logging system.
        /// Ensures the log directory and file exist, then truncates it to the last N lines.
        /// </summary>
        /// <param name="truncate">
        /// Number of lines to keep from the end of the file. Defaults to 1000.
        /// Use 0 to clear the file completely, or a negative value to skip truncation.
        /// </param>
        public static void Initialize(int truncate = 1000)
        {
            try
            {
                // Ensure directory and file exist
                lock (_fileLock)
                {
                    var dir = Path.GetDirectoryName(LogFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    if (!File.Exists(LogFile))
                    {
                        using (File.Create(LogFile))
                        {
                            // Just create the file; no content needed.
                        }
                    }
                }

                // Truncate if requested (Truncate will re-acquire _fileLock internally)
                if (truncate >= 0)
                    Truncate(truncate);
            }
            catch
            {
                // Never let logging initialization break the game.
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Trace(string message) => Write(LogLevel.Trace, message);

        public static void Debug(string message) => Write(LogLevel.Debug, message);

        public static void Info(string message) => Write(LogLevel.Info, message);

        public static void Success(string message) => Write(LogLevel.Success, message);

        public static void Warn(string message) => Write(LogLevel.Warn, message);

        public static void Error(string message) => Write(LogLevel.Error, message);

        public static void Critical(string message) => Write(LogLevel.Critical, message);

        /// <summary>
        /// Dumps an object structure to the log for quick inspection.
        /// </summary>
        public static void Dump(object obj, LogLevel level = LogLevel.Debug)
        {
            var sb = new StringBuilder();

            void DumpRecursive(object o, int depth)
            {
                if (o == null)
                {
                    sb.Append("<null> ");
                    return;
                }

                var type = o.GetType();

                // Handle IDictionary
                if (o is IDictionary dict)
                {
                    var genericArgs = type.GetGenericArguments();
                    string keyType = genericArgs.Length > 0 ? genericArgs[0].Name : "object";
                    string valueType = genericArgs.Length > 1 ? genericArgs[1].Name : "object";

                    sb.Append($"Dictionary<{keyType},{valueType}>[{dict.Count}] ");

                    foreach (DictionaryEntry entry in dict)
                    {
                        sb.Append("Key: ");
                        DumpRecursive(entry.Key, depth + 1);
                        sb.Append("Value: ");
                        DumpRecursive(entry.Value, depth + 1);
                    }

                    return;
                }

                // Handle IEnumerable (but not string)
                if (o is IEnumerable enumerable && o is not string)
                {
                    var elementType = type.IsArray
                        ? type.GetElementType()
                        : type.GetGenericArguments().FirstOrDefault();

                    var elementName = elementType?.Name ?? type.Name;

                    sb.Append($"List<{elementName}>: ");

                    int i = 0;
                    foreach (var item in enumerable)
                    {
                        sb.Append($"[{i}]: ");
                        DumpRecursive(item, depth + 1);
                        i++;
                    }

                    if (i == 0)
                        sb.Append("<empty> ");

                    return;
                }

                // Fallback: ToString
                try
                {
                    var str = o.ToString();
                    if (string.IsNullOrEmpty(str))
                        str = $"<{type.FullName}>";

                    sb.Append($"{str} ");
                }
                catch (Exception ex)
                {
                    sb.Append($"Log.Dump failed for object of type {type.FullName}: {ex.Message} ");
                }
            }

            DumpRecursive(obj, 0);
            Write(level, sb.ToString());
        }

        /// <summary>
        /// Logs an exception with a rich stack trace and any inner exceptions.
        /// </summary>
        public static void Exception(Exception ex, string context = "", string caller = null)
        {
            if (ex == null)
                return;

            var sb = new StringBuilder();
            sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);

            if (!string.IsNullOrWhiteSpace(context))
                sb.Append(" | ").Append(context);

            sb.AppendLine();

            AppendStackTrace(sb, ex);

            if (ex.Data is { Count: > 0 })
            {
                sb.AppendLine("Data:");
                foreach (DictionaryEntry kv in ex.Data)
                    sb.Append("  ").Append(kv.Key).Append(": ").AppendLine(kv.Value?.ToString());
            }

            Write(LogLevel.Error, sb.ToString(), caller);

            var inner = ex.InnerException;
            int depth = 1;

            while (inner != null)
            {
                var innerBuilder = new StringBuilder();
                innerBuilder
                    .Append("[INNER ")
                    .Append(depth)
                    .Append("] ")
                    .Append(inner.GetType().Name)
                    .Append(": ")
                    .AppendLine(inner.Message);

                AppendStackTrace(innerBuilder, inner);

                Write(LogLevel.Error, innerBuilder.ToString(), caller);

                inner = inner.InnerException;
                depth++;
            }
        }

        /// <summary>
        /// Logs the caller located N frames up the stack for quick tracing.
        /// </summary>
        public static void Trace(int up = 1, LogLevel level = LogLevel.Debug)
        {
            var parent = Caller.GetCallerAboveLabel(up: up);
            Write(level, $"Called by: {parent}");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Stack Trace                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void AppendStackTrace(StringBuilder sb, Exception ex)
        {
            sb.AppendLine("Exception stack:");

            var st = new StackTrace(ex, true);
            var frames = st.GetFrames() ?? Array.Empty<StackFrame>();

            foreach (var f in frames)
            {
                var m = f.GetMethod();
                if (m == null)
                    continue;

                var t = m.DeclaringType;
                var typeName = t?.FullName ?? "<unknown>";
                var methodName = m.Name;

                var pars = m.GetParameters();
                var sig = string.Join(", ", pars.Select(p => p.ParameterType.Name + " " + p.Name));

                var file = f.GetFileName();
                var line = f.GetFileLineNumber();
                var col = f.GetFileColumnNumber();

                sb.Append("  at ")
                    .Append(typeName)
                    .Append(".")
                    .Append(methodName)
                    .Append("(")
                    .Append(sig)
                    .Append(")");

                if (!string.IsNullOrEmpty(file) && line > 0)
                {
                    sb.Append(" in ")
                        .Append(file)
                        .Append(":line ")
                        .Append(line)
                        .Append(":col ")
                        .Append(col);
                }

                sb.AppendLine();
            }

            if (frames.Length == 0 && !string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine("(raw ex.StackTrace follows)");
                sb.AppendLine(ex.StackTrace);
            }

            var managed = PruneManagedStack(Environment.StackTrace);
            if (!string.IsNullOrEmpty(managed))
            {
                sb.AppendLine("Managed call stack:");
                sb.AppendLine(managed);
            }
        }

        private static string PruneManagedStack(string envStack, int maxLines = 100)
        {
            if (string.IsNullOrEmpty(envStack))
                return null;

            var skipTopHints = new[]
            {
                // BCL plumbing
                "System.Environment.GetStackTrace",
                "System.Environment.get_StackTrace",
                // Logging plumbing
                "Retinues.Utilities.Log.AppendStackTrace",
                "Retinues.Utilities.Log.Exception",
                "Retinues.Utilities.Log.Error",
                "Retinues.Utilities.Caller",
                // Generic safe wrappers
                "SafeMethodPatcher",
                "SafeMethod",
                "SafeClass",
                // Harmony scaffolding
                "HarmonyLib.",
                // JIT helpers / runtime
                "System.RuntimeMethodHandle",
                "System.Reflection.RuntimeMethodInfo",
            };

            var lines = envStack
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimEnd())
                .ToList();

            int start = 0;
            while (start < lines.Count && ContainsAny(lines[start], skipTopHints))
                start++;

            if (start >= lines.Count)
                return null;

            while (
                start < lines.Count
                && lines[start].IndexOf("HarmonyLib.", StringComparison.Ordinal) >= 0
            )
            {
                start++;
            }

            var kept = lines.Skip(start).Take(maxLines);
            return string.Join(Environment.NewLine, kept);
        }

        private static bool ContainsAny(string line, string[] hints)
        {
            foreach (var h in hints)
            {
                if (line.IndexOf(h, StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Writers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void Write(LogLevel level, string message, string caller = null)
        {
            caller ??= Caller.GetLabel();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{level}] {caller}: {message}";

            if (level >= MinFileLevel)
                WriteToFile(line);

            if (level >= MinInGameLevel)
                WriteInGame(message, level);
        }

        private static void WriteInGame(string message, LogLevel level)
        {
            var color = LevelColor(level);
            var text = InGamePrefix + message;
            InformationManager.DisplayMessage(new InformationMessage(text, color));
        }

        private static void WriteToFile(string line)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Swallow file errors to never break gameplay
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Colors                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Color LevelColor(LogLevel level)
        {
            var colorString = level switch
            {
                LogLevel.Trace => "#9E9E9EFF",
                LogLevel.Debug => "#64B5F6FF",
                LogLevel.Info => "#2196F3FF",
                LogLevel.Success => "#43A047FF",
                LogLevel.Warn => "#FFA000FF",
                LogLevel.Error => "#E53935FF",
                LogLevel.Critical => "#B71C1CFF",
                _ => "#ffffffff",
            };

            return Color.ConvertStringToColor(colorString);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Truncation                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the current number of lines in the log file.
        /// </summary>
        public static int LogFileLength
        {
            get
            {
                try
                {
                    lock (_fileLock)
                    {
                        if (!File.Exists(LogFile))
                            return 0;

                        return File.ReadLines(LogFile, Encoding.UTF8).Count();
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Truncates the log file, keeping only the last N lines.
        /// </summary>
        public static void Truncate(int keepLastNLines)
        {
            if (keepLastNLines < 0)
                keepLastNLines = 0;

            try
            {
                lock (_fileLock)
                {
                    if (!File.Exists(LogFile))
                        return;

                    if (keepLastNLines == 0)
                    {
                        File.WriteAllText(LogFile, string.Empty, Encoding.UTF8);
                        return;
                    }

                    var ring = new Queue<string>(keepLastNLines);

                    using (
                        var fs = new FileStream(
                            LogFile,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite
                        )
                    )
                    using (
                        var sr = new StreamReader(
                            fs,
                            Encoding.UTF8,
                            detectEncodingFromByteOrderMarks: true
                        )
                    )
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (ring.Count == keepLastNLines)
                                ring.Dequeue();

                            ring.Enqueue(line);
                        }
                    }

                    var tmp = LogFile + ".tmp";

                    using (var sw = new StreamWriter(tmp, false, Encoding.UTF8))
                    {
                        bool first = true;
                        foreach (var l in ring)
                        {
                            if (!first)
                                sw.Write(Environment.NewLine);

                            sw.Write(l);
                            first = false;
                        }
                    }

                    File.Copy(tmp, LogFile, overwrite: true);
                    File.Delete(tmp);
                }
            }
            catch
            {
                // ignore truncation failures
            }
        }
    }
}
