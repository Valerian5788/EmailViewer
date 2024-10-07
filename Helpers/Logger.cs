using System;
using System.IO;
using System.Linq;

namespace EmailViewer.Helpers
{
    public static class Logger
    {
        private const int MaxLogFiles = 7;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public static void Log(string message)
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            string currentLogPath = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");

            if (File.Exists(currentLogPath) && new FileInfo(currentLogPath).Length > MaxFileSizeBytes)
            {
                RotateLogs(logDirectory);
            }

            File.AppendAllText(currentLogPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        private static void RotateLogs(string logDirectory)
        {
            var logFiles = Directory.GetFiles(logDirectory, "app_*.log")
                                    .OrderByDescending(f => f)
                                    .ToList();

            for (int i = logFiles.Count - 1; i >= 0; i--)
            {
                if (i >= MaxLogFiles - 1)
                {
                    File.Delete(logFiles[i]);
                }
                else
                {
                    File.Move(logFiles[i], logFiles[i].Replace(".log", $"_{i + 1}.log"));
                }
            }
        }
    }
}