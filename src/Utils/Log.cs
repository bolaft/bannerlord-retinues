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
            var formatted = $"[{timestamp}] {message}";
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
    }
}
