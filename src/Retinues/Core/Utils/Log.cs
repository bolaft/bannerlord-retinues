using System;
using System.IO;
using System.Diagnostics;
using TaleWorlds.Library;

namespace Retinues.Core.Utils
{
    public enum LogLevel
    {
        Trace    = 0,
        Debug    = 1,
        Info     = 2,
        Success  = 3,
        Warn     = 4,
        Error    = 5,
        Critical = 6
    }

    public static class Log
    {
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
        /*                           Log Level Configuration                          */
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

        // Lowest level written to file
        public const LogLevel MinFileLevel = LogLevel.Debug;

        // Lowest level shown in-game (InformationManager)
        public const LogLevel MinInGameLevel = LogLevel.Info;

        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
        /*                               Log File Setup                               */
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

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

        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
        /*                                 Public API                                 */
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

        public static void Trace(string message)    => Write(LogLevel.Trace,   message);
        public static void Debug(string message)    => Write(LogLevel.Debug,   message);
        public static void Info(string message)     => Write(LogLevel.Info,    message);
        public static void Success(string message)  => Write(LogLevel.Success, message);
        public static void Warn(string message)     => Write(LogLevel.Warn,    message);
        public static void Error(string message)    => Write(LogLevel.Error,   message);
        public static void Critical(string message) => Write(LogLevel.Critical,message);

        public static void Exception(Exception ex, string context = "")
        {
            var msg =
                $"[EXCEPTION] {ex.GetType().Name}: {ex.Message}" +
                (string.IsNullOrWhiteSpace(context) ? "" : $" | {context}") +
                Environment.NewLine + ex.StackTrace;

            Write(LogLevel.Error, msg);
            // Include inner exceptions if present
            var inner = ex.InnerException;
            while (inner != null)
            {
                Write(LogLevel.Error, $"[INNER] {inner.GetType().Name}: {inner.Message}{Environment.NewLine}{inner.StackTrace}");
                inner = inner.InnerException;
            }
        }

        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
        /*                                   Writers                                  */
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

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

        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */
        /*                                   Helpers                                  */
        /* ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ */

        private static Color LevelColor(LogLevel level)
        {
            // Palette tuned for readability on most backgrounds
            switch (level)
            {
                case LogLevel.Trace:    return FromHex("#9E9E9EFF"); // grey 600
                case LogLevel.Debug:    return FromHex("#64B5F6FF"); // light blue
                case LogLevel.Info:     return FromHex("#2196F3FF"); // blue
                case LogLevel.Success:  return FromHex("#43A047FF"); // green
                case LogLevel.Warn:     return FromHex("#FFA000FF"); // amber
                case LogLevel.Error:    return FromHex("#E53935FF"); // red
                case LogLevel.Critical: return FromHex("#B71C1CFF"); // deep red
                default:                return Color.White;
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
                    if (type == null) continue;

                    if (type == typeof(Log)) continue; // skip logger frames

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
