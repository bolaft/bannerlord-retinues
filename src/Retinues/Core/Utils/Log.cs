using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
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
        public static LogLevel MinFileLevel => Config.GetOption<bool>("DebugMode") ? LogLevel.Debug : LogLevel.Info;

        // Lowest level shown in-game (InformationManager)
        public static LogLevel MinInGameLevel => Config.GetOption<bool>("DebugMode") ? LogLevel.Info : LogLevel.Critical;

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
                var asmDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                )!;
                // e.g. .../Modules/YourModule/bin/Win64_Shipping_Client
                // -> go up twice to module root
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

        public static void Exception(Exception ex, string context = "")
        {
            var sb = new StringBuilder();
            sb.Append("[EXCEPTION] ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            if (!string.IsNullOrWhiteSpace(context))
                sb.Append(" | ").Append(context);
            sb.AppendLine();

            // Rich stack trace (with file/line when PDBs exist)
            AppendStackTrace(sb, ex);

            // Extra exception metadata
            sb.AppendLine("Source: " + ex.Source);
            sb.AppendLine("TargetSite: " + ex.TargetSite);
            sb.AppendLine("HResult: 0x" + ex.HResult.ToString("X"));

            // Exception.Data
            if (ex.Data?.Count > 0)
            {
                sb.AppendLine("Data:");
                foreach (DictionaryEntry kv in ex.Data)
                    sb.Append("  ").Append(kv.Key).Append(": ").AppendLine(kv.Value?.ToString());
            }

            Write(LogLevel.Error, sb.ToString());

            // Chain inner exceptions (recursively, with rich stacks)
            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                var ib = new StringBuilder();
                ib.Append("[INNER ").Append(depth).Append("] ").Append(inner.GetType().Name)
                .Append(": ").AppendLine(inner.Message);
                AppendStackTrace(ib, inner);
                Write(LogLevel.Error, ib.ToString());
                inner = inner.InnerException;
                depth++;
            }
        }

        private static void AppendStackTrace(StringBuilder sb, Exception ex)
        {
            // 'true' => capture file info if available
            var st = new StackTrace(ex, true);
            var frames = st.GetFrames() ?? Array.Empty<StackFrame>();

            // Optional: prioritize frames from your assemblies
            string[] prefer = { "Retinues.", "YourModNamespace." };

            foreach (var f in frames)
            {
                var method = f.GetMethod();
                if (method == null) continue;

                var type = method.DeclaringType;
                var typeName = type?.FullName ?? "<unknown>";
                var methodName = method.Name;

                // Params
                var pars = method.GetParameters();
                var sig = string.Join(", ", pars.Select(p => p.ParameterType.Name + " " + p.Name));

                // File/line/col (only if PDBs available)
                var file = f.GetFileName();
                var line = f.GetFileLineNumber();
                var col  = f.GetFileColumnNumber();

                sb.Append("  at ").Append(typeName).Append(".").Append(methodName).Append("(").Append(sig).Append(")");

                if (!string.IsNullOrEmpty(file) && line > 0)
                    sb.Append(" in ").Append(file).Append(":line ").Append(line).Append(":col ").Append(col);

                sb.AppendLine();
            }

            // If no frames (no PDBs), fall back to default
            if (frames.Length == 0)
                sb.AppendLine(ex.StackTrace);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Writers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void Write(LogLevel level, string message)
        {
            var caller = FindCaller();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{level}] {caller}{message}";

            if (level >= MinFileLevel)
                WriteToFile(line);

            if (level >= MinInGameLevel)
                WriteInGame($"{caller}{message}", level);
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

        private static string FindCaller()
        {
            try
            {
                var stack = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
                for (int i = 0; i < stack.FrameCount; i++)
                {
                    var frame = stack.GetFrame(i);
                    var method = frame?.GetMethod();
                    var type = method?.DeclaringType;
                    if (type == null)
                        continue;

                    if (type == typeof(Log))
                        continue; // skip logger frames

                    var typeName = type.Name;
                    var methodName = method!.Name is ".ctor" or ".cctor" ? typeName : method.Name;
                    return $"{typeName}.{methodName}: ";
                }
            }
            catch
            {
                // ignore stack issues
            }
            return "";
        }
    }
}
