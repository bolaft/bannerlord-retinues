using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.Library;

namespace Retinues.Core.Utils
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

    public static class Log
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Log Level Configuration                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Lowest level written to file
        public static LogLevel MinFileLevel =>
            Config.GetOption<bool>("DebugMode") ? LogLevel.Debug : LogLevel.Info;

        // Lowest level shown in-game (InformationManager)
        public static LogLevel MinInGameLevel =>
            Config.GetOption<bool>("DebugMode") ? LogLevel.Info : LogLevel.Critical;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Log File Setup                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const string LogFileName = "debug.log";
        private static readonly object _fileLock = new();

        private static readonly string LogFile;

        static Log()
        {
            try
            {
                var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                // Go up twice to module root
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                LogFile = Path.Combine(moduleRoot, LogFileName);
            }
            catch
            {
                // Fallback to process working dir if the above fails
                LogFile = LogFileName;
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

        public static void Exception(Exception ex, string context = "", string caller = null)
        {
            var sb = new StringBuilder();
            sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            if (!string.IsNullOrWhiteSpace(context))
                sb.Append(" | ").Append(context);
            sb.AppendLine();

            // Rich stack trace (with file/line when PDBs exist)
            AppendStackTrace(sb, ex);

            // Exception.Data
            if (ex.Data?.Count > 0)
            {
                sb.AppendLine("Data:");
                foreach (DictionaryEntry kv in ex.Data)
                    sb.Append("  ").Append(kv.Key).Append(": ").AppendLine(kv.Value?.ToString());
            }

            Write(LogLevel.Error, sb.ToString(), caller: caller);

            // Chain inner exceptions (recursively, with rich stacks)
            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                var ib = new StringBuilder();
                ib.Append("[INNER ")
                    .Append(depth)
                    .Append("] ")
                    .Append(inner.GetType().Name)
                    .Append(": ")
                    .AppendLine(inner.Message);
                AppendStackTrace(ib, inner);
                Write(LogLevel.Error, ib.ToString(), caller: caller);
                inner = inner.InnerException;
                depth++;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Stack Trace                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void AppendStackTrace(StringBuilder sb, Exception ex)
        {
            // 1) Symbol-aware exception stack (same as before)
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
                    sb.Append(" in ")
                        .Append(file)
                        .Append(":line ")
                        .Append(line)
                        .Append(":col ")
                        .Append(col);

                sb.AppendLine();
            }

            if (frames.Length == 0 && !string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine("(raw ex.StackTrace follows)");
                sb.AppendLine(ex.StackTrace);
            }

            // 2) Managed call stack at the catch site, pruned
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

            // Things we never want at the top of the managed stack
            // (add/remove entries as needed)
            var skipTopHints = new[]
            {
                // BCL plumbing
                "System.Environment.GetStackTrace",
                "System.Environment.get_StackTrace",
                // Logging plumbing
                "Retinues.Core.Utils.Log.AppendStackTrace",
                "Retinues.Core.Utils.Log.Exception",
                "Retinues.Core.Utils.Log.Error",
                "Retinues.Core.Utils.Caller",
                // Patcher wrappers
                "Retinues.Core.Utils.SafeMethodPatcher",
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

            // Find the first frame that is NOT one of the "skipTopHints"
            int start = 0;
            while (start < lines.Count && ContainsAny(lines[start], skipTopHints))
                start++;

            if (start >= lines.Count)
                return null;

            // Optionally also drop any *immediate* Harmony wrappers that follow
            while (
                start < lines.Count
                && lines[start].IndexOf("HarmonyLib.", StringComparison.Ordinal) >= 0
            )
                start++;

            // Cap length to keep logs readable
            var kept = lines.Skip(start).Take(maxLines);

            return string.Join(Environment.NewLine, kept);
        }

        private static bool ContainsAny(string line, string[] hints)
        {
            foreach (var h in hints)
                if (line.IndexOf(h, StringComparison.Ordinal) >= 0)
                    return true;
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
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }

        private static void WriteToFile(string line)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // swallow file errors to never break gameplay
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static Color LevelColor(LogLevel level)
        {
            // Palette tuned for readability on most backgrounds
            switch (level)
            {
                case LogLevel.Trace:
                    return FromHex("#9E9E9EFF"); // grey 600
                case LogLevel.Debug:
                    return FromHex("#64B5F6FF"); // light blue
                case LogLevel.Info:
                    return FromHex("#2196F3FF"); // blue
                case LogLevel.Success:
                    return FromHex("#43A047FF"); // green
                case LogLevel.Warn:
                    return FromHex("#FFA000FF"); // amber
                case LogLevel.Error:
                    return FromHex("#E53935FF"); // red
                case LogLevel.Critical:
                    return FromHex("#B71C1CFF"); // deep red
                default:
                    return Color.White;
            }
        }

        private static Color FromHex(string rrggbbaa)
        {
            return Color.ConvertStringToColor(rrggbbaa);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Truncation                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

                        return File.ReadLines(LogFile).Count();
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static void Truncate(int keepLastNLines)
        {
            if (keepLastNLines < 0) keepLastNLines = 0;

            try
            {
                lock (_fileLock)
                {
                    if (!File.Exists(LogFile))
                        return;

                    // Fast path: clear the file if asked to keep 0 lines
                    if (keepLastNLines == 0)
                    {
                        File.WriteAllText(LogFile, string.Empty);
                        return;
                    }

                    // Ring buffer of the last N lines
                    var ring = new Queue<string>(keepLastNLines);

                    using (var fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (ring.Count == keepLastNLines) ring.Dequeue();
                            ring.Enqueue(line);
                        }
                    }

                    // Write back via temp file, then swap in
                    var tmp = LogFile + ".tmp";
                    using (var sw = new StreamWriter(tmp, false, Encoding.UTF8))
                    {
                        bool first = true;
                        foreach (var l in ring)
                        {
                            if (!first) sw.Write(Environment.NewLine);
                            sw.Write(l);
                            first = false;
                        }
                    }

                    // Replace original content
                    File.Copy(tmp, LogFile, overwrite: true);
                    File.Delete(tmp);
                }
            }
            catch { }
        }
    }
}
