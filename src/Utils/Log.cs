using System;
using System.IO;
using TaleWorlds.Library;

namespace CustomClanTroops.Utils
{
    public class Log
    {
        private static readonly string LogFile;

        static Log()
        {
            try
            {
                var asmDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                LogFile = Path.Combine(moduleRoot, "CustomClanTroops.log");
            }
            catch
            {
                LogFile = "CustomClanTroops.log";
            }
        }

        public static void WriteToInGameChat(string message, string color = "white")
        {
            // Implementation for writing to in-game chat
            var formatted = $"[CCT] {message}";
            InformationManager.DisplayMessage(new InformationMessage(formatted));
        }

        public static void WriteToLogFile(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var caller = FindCaller();

            var formatted = $"[{timestamp}] {caller}{message}";
            try
            {
                File.AppendAllText(LogFile, formatted + Environment.NewLine);
            }
            catch { /* Ignore file errors */ }
        }

        private static void Write(string message, string color = "white")
        {
            WriteToInGameChat(message, color);
            WriteToLogFile(message);
        }

        public static void Info(string message) => Write(message, "white");

        public static void Success(string message) => Write(message, "green");

        public static void Error(string message) => Write(message, "red");

        public static void Warn(string message) => Write(message, "yellow");

        // Debug messages don't show in game, only in log file
        public static void Debug(string message) => WriteToLogFile(message);

        private static string FindCaller()
        {
            try
            {
                var stack = new System.Diagnostics.StackTrace();
                // 0 = FindCaller, 1 = WriteToLogFile, 2 = Log.Debug/Info/etc, 3 = actual caller
                var frame = stack.GetFrame(3);
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    if (method != null)
                    {
                        var className = method.DeclaringType != null ? method.DeclaringType.Name : "<UnknownClass>";
                        var methodName = method.Name;
                        return $"{className}.{methodName}: ";
                    }
                }
            }
            catch { /* Ignore stack errors */ }
            return "";
        }
    }
}
