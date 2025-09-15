using System;
using System.IO;
using TaleWorlds.Library;

namespace Retinues.Core.Utils
{
    public class Log
    {
        private static readonly string LogFile;

        static Log()
        {
            try
            {
                var asmDir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                )!;
                var moduleRoot = Directory.GetParent(asmDir)!.Parent!.FullName;
                LogFile = Path.Combine(moduleRoot, "debug.log");
            }
            catch
            {
                LogFile = "debug.log";
            }
        }

        public static void WriteToInGameChat(string message, string color = "white")
        {
            // Implementation for writing to in-game chat
            var formatted = $"{message}";
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
            catch
            { /* Ignore file errors */
            }
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

        // Exception logger
        public static void Exception(Exception ex)
        {
            Error($"[EXCEPTION] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
        }

        private static string FindCaller()
        {
            try
            {
                var stack = new System.Diagnostics.StackTrace();
                for (int i = 1; i < stack.FrameCount; i++)
                {
                    var frame = stack.GetFrame(i);
                    var method = frame?.GetMethod();
                    var className = method?.DeclaringType?.Name;
                    if (className != null && className != nameof(Log))
                    {
                        var methodName =
                            method.Name == ".ctor" || method.Name == ".cctor"
                                ? className
                                : method.Name;
                        return $"{className}.{methodName}: ";
                    }
                }
            }
            catch
            { /* Ignore stack errors */
            }
            return "";
        }
    }
}
