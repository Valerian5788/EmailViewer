using System;
using System.IO;

namespace EmailViewer.Tests
{
    public static class LoggerTest
    {
        private const string LogFile = "test_log.txt";

        public static void Log(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFile);

            try
            {
                File.AppendAllText(fullPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public static void ClearLog()
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFile);
            try
            {
                File.WriteAllText(fullPath, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing log file: {ex.Message}");
            }
        }
    }
}